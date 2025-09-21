using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// DSP ��� ��Ʈ�γ�: PlayScheduled�� Ŭ���� ���� ���.
/// - ��Ȯ�� ����� ��ũ
/// - ����/����/�������� �ǽð� ���� ����
/// - �Ǽ�Ʈ/�Ϲ� Ŭ�� ����
/// - Ŭ�� ���� ������ �� ��ü ���� Ŭ�� ���
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class MetronomeDSP : MonoBehaviour
{
    [Header("Tempo / Meter")]
    [Range(20f, 300f)] public float bpm = 120f;   // �д� ��Ʈ
    [Range(1, 16)] public int beatsPerBar = 4;    // ����� ����
    [Range(1, 8)] public int subdivision = 1;     // ���ڴ� �ɰ���(��: 2=8����ǥ, 3=�����, 4=16����ǥ)

    [Header("Click Sounds (optional)")]
    public AudioClip accentClip;   // ���� ù ���� Ŭ��
    public AudioClip tickClip;     // �Ϲ� ����/�������� Ŭ��
    [Range(0f, 1f)] public float accentVolume = 0.9f;
    [Range(0f, 1f)] public float tickVolume = 0.6f;

    [Header("Scheduling")]
    [Tooltip("�̸� ������ �ð�(��). ����� ����/������ ������ ����� 0.05~0.2 ����")]
    public double scheduleAheadTime = 0.12;
    [Tooltip("�� ����ŭ�� dspTime�� �����Ű�� ���� ������ ä���")]
    public double scheduleStepSafety = 0.01;

    [Header("State (ReadOnly)")]
    public bool isPlaying = false;
    public int currentBar = 0;         // 0���� ����
    public int currentBeatInBar = 0;   // 0..beatsPerBar-1
    public int currentSubIndex = 0;    // 0..subdivision-1

    private double nextTickDspTime;    // ���� Ŭ�� ���� �ð�
    private double secondsPerBeat;     // 60/bpm
    private double secondsPerSub;      // secondsPerBeat / subdivision

    // �� ���� ������ҽ��� ������ ����(���� Ŭ���� ��ĥ �� ���� ����)
    private AudioSource srcA;
    private AudioSource srcB;
    private bool useA = true;

    // Ŭ�� ������ �� �����ϴ� ���� Ŭ��
    private AudioClip accentGen;
    private AudioClip tickGen;

    void Awake()
    {
        // ���� ������ҽ��� '������'�����θ� ��. ���� ������ A/B �ҽ� ���
        var main = GetComponent<AudioSource>();
        main.playOnAwake = false;

        // A/B �ҽ� �غ�
        srcA = gameObject.AddComponent<AudioSource>();
        srcB = gameObject.AddComponent<AudioSource>();
        foreach (var s in new[] { srcA, srcB })
        {
            s.playOnAwake = false;
            s.loop = false;
        }

        // �⺻ ���� �Ļ� �� ���
        RecalcIntervals();

        // Ŭ�� ����(�ʿ��� ���)
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
    /// ª�� Ŭ������ �������� ����
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
            float env = Mathf.Exp(-3f * i / Mathf.Max(1, decaySamples)); // ���� ����
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
        nextTickDspTime = dspNow + 0.05f; // �ణ �ڿ��� ���� (���� ����)
        ResetCounters();

        isPlaying = true;
    }

    public void Stop()
    {
        isPlaying = false;
        // ���� ���� ���� ���� ��� ���� �ҽ��� ����
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
        // ����/����/���������� �ν����Ϳ��� �ٲ� �� �����Ƿ� �� ������ Ȯ��
        RecalcIntervals();

        if (!isPlaying) return;

        double dspNow = AudioSettings.dspTime;

        // ���� �ð� �ձ��� ���� ���� ����
        while (nextTickDspTime < dspNow + scheduleAheadTime)
        {
            bool isAccent = (currentBeatInBar == 0) && (currentSubIndex == 0);

            ScheduleOneClick(nextTickDspTime, isAccent);

            // ���� ������� �ð����� �̵�
            nextTickDspTime += secondsPerSub;

            // ī���� ����
            AdvanceCounters();

            // ���� ���� ����(���ѷ��� ����)
            dspNow = AudioSettings.dspTime;
            nextTickDspTime += scheduleStepSafety * 0.0; // �ǹ̻� ���� ����, 0�̸� ���� ����
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

    // �ܺο��� ������ �ٲٰ� ���� �� ȣ��
    public void SetTempo(float newBpm, bool keepPhase = true)
    {
        bpm = Mathf.Clamp(newBpm, 20f, 300f);
        RecalcIntervals();

        if (!isPlaying) return;

        if (!keepPhase)
        {
            // �� ������ �ٷ� ���� �ʱ�ȭ
            nextTickDspTime = AudioSettings.dspTime + 0.05;
            ResetCounters();
        }
        // keepPhase=true �� ���, ���� ������� �� �������� �ڿ������� �̾���
    }
}
