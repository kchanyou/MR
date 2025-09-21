using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// DSP 기반 메트로놈: PlayScheduled로 클릭을 예약 재생.
/// - 정확한 오디오 싱크
/// - 템포/박자/서브디비전 실시간 변경 지원
/// - 악센트/일반 클릭 지원
/// - 클릭 샘플 미지정 시 자체 생성 클릭 사용
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class MetronomeDSP : MonoBehaviour
{
    [Header("Tempo / Meter")]
    [Range(20f, 300f)] public float bpm = 120f;   // 분당 비트
    [Range(1, 16)] public int beatsPerBar = 4;    // 마디당 박자
    [Range(1, 8)] public int subdivision = 1;     // 박자당 쪼개기(예: 2=8분음표, 3=삼분할, 4=16분음표)

    [Header("Click Sounds (optional)")]
    public AudioClip accentClip;   // 마디 첫 박자 클릭
    public AudioClip tickClip;     // 일반 박자/서브디비전 클릭
    [Range(0f, 1f)] public float accentVolume = 0.9f;
    [Range(0f, 1f)] public float tickVolume = 0.6f;

    [Header("Scheduling")]
    [Tooltip("미리 예약할 시간(초). 오디오 버퍼/프레임 지연을 고려해 0.05~0.2 권장")]
    public double scheduleAheadTime = 0.12;
    [Tooltip("이 값만큼씩 dspTime을 진행시키며 예약 루프를 채운다")]
    public double scheduleStepSafety = 0.01;

    [Header("State (ReadOnly)")]
    public bool isPlaying = false;
    public int currentBar = 0;         // 0부터 시작
    public int currentBeatInBar = 0;   // 0..beatsPerBar-1
    public int currentSubIndex = 0;    // 0..subdivision-1

    private double nextTickDspTime;    // 다음 클릭 예약 시각
    private double secondsPerBeat;     // 60/bpm
    private double secondsPerSub;      // secondsPerBeat / subdivision

    // 두 개의 오디오소스로 번갈아 예약(여러 클릭이 겹칠 때 꼬임 방지)
    private AudioSource srcA;
    private AudioSource srcB;
    private bool useA = true;

    // 클릭 미지정 시 생성하는 내부 클릭
    private AudioClip accentGen;
    private AudioClip tickGen;

    void Awake()
    {
        // 메인 오디오소스를 '프리뷰'용으로만 둠. 실제 예약은 A/B 소스 사용
        var main = GetComponent<AudioSource>();
        main.playOnAwake = false;

        // A/B 소스 준비
        srcA = gameObject.AddComponent<AudioSource>();
        srcB = gameObject.AddComponent<AudioSource>();
        foreach (var s in new[] { srcA, srcB })
        {
            s.playOnAwake = false;
            s.loop = false;
        }

        // 기본 템포 파생 값 계산
        RecalcIntervals();

        // 클릭 생성(필요한 경우)
        EnsureGeneratedClicks();
    }

    void RecalcIntervals()
    {
        secondsPerBeat = 60.0 / Mathf.Max(1f, bpm);
        secondsPerSub = secondsPerBeat / Mathf.Max(1, subdivision);
    }

    void EnsureGeneratedClicks()
    {
        int sampleRate = AudioSettings.outputSampleRate;

        if (accentClip == null)
            accentGen = CreateClick(sampleRate, lengthMs: 8, freq: 1000, decayMs: 40);
        if (tickClip == null)
            tickGen = CreateClick(sampleRate, lengthMs: 6, freq: 2000, decayMs: 30);
    }

    /// <summary>
    /// 짧은 클릭음을 동적으로 생성
    /// </summary>
    AudioClip CreateClick(int sampleRate, int lengthMs, float freq, int decayMs)
    {
        int length = Mathf.CeilToInt(sampleRate * (lengthMs / 1000f));
        float[] data = new float[length];

        float phase = 0f;
        float phaseInc = 2f * Mathf.PI * freq / sampleRate;
        int decaySamples = Mathf.CeilToInt(sampleRate * (decayMs / 1000f));

        for (int i = 0; i < length; i++)
        {
            float env = Mathf.Exp(-3f * i / Mathf.Max(1, decaySamples)); // 지수 감쇠
            data[i] = Mathf.Sin(phase) * env;
            phase += phaseInc;
        }

        var clip = AudioClip.Create($"click_{freq}Hz", length, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    public void Play()
    {
        if (isPlaying) return;

        RecalcIntervals();
        EnsureGeneratedClicks();

        double dspNow = AudioSettings.dspTime;
        nextTickDspTime = dspNow + 0.05f; // 약간 뒤에서 시작 (안전 여유)
        ResetCounters();

        isPlaying = true;
    }

    public void Stop()
    {
        isPlaying = false;
        // 예약 끊기 위해 현재 재생 중인 소스도 정지
        srcA.Stop();
        srcB.Stop();
    }

    void ResetCounters()
    {
        currentBar = 0;
        currentBeatInBar = 0;
        currentSubIndex = 0;
    }

    void Update()
    {
        // 템포/마디/서브디비전이 인스펙터에서 바뀔 수 있으므로 매 프레임 확인
        RecalcIntervals();

        if (!isPlaying) return;

        double dspNow = AudioSettings.dspTime;

        // 일정 시간 앞까지 예약 루프 실행
        while (nextTickDspTime < dspNow + scheduleAheadTime)
        {
            bool isAccent = (currentBeatInBar == 0) && (currentSubIndex == 0);

            ScheduleOneClick(nextTickDspTime, isAccent);

            // 다음 서브박자 시간으로 이동
            nextTickDspTime += secondsPerSub;

            // 카운터 진행
            AdvanceCounters();

            // 안전 루프 중폭(무한루프 방지)
            dspNow = AudioSettings.dspTime;
            nextTickDspTime += scheduleStepSafety * 0.0; // 의미상 안전 변수, 0이면 영향 없음
        }
    }

    void AdvanceCounters()
    {
        currentSubIndex++;
        if (currentSubIndex >= subdivision)
        {
            currentSubIndex = 0;
            currentBeatInBar++;
            if (currentBeatInBar >= beatsPerBar)
            {
                currentBeatInBar = 0;
                currentBar++;
            }
        }
    }

    void ScheduleOneClick(double dspWhen, bool accent)
    {
        var clip = accent ? (accentClip != null ? accentClip : accentGen)
                          : (tickClip != null ? tickClip : tickGen);
        float vol = accent ? accentVolume : tickVolume;

        var src = useA ? srcA : srcB;
        useA = !useA;

        src.clip = clip;
        src.volume = vol;
        src.PlayScheduled(dspWhen);
    }

    // 외부에서 템포를 바꾸고 싶을 때 호출
    public void SetTempo(float newBpm, bool keepPhase = true)
    {
        bpm = Mathf.Clamp(newBpm, 20f, 300f);
        RecalcIntervals();

        if (!isPlaying) return;

        if (!keepPhase)
        {
            // 새 템포로 바로 위상 초기화
            nextTickDspTime = AudioSettings.dspTime + 0.05;
            ResetCounters();
        }
        // keepPhase=true 인 경우, 다음 예약부터 새 간격으로 자연스럽게 이어짐
    }
}
