using UnityEngine;
using UnityEngine.Audio;
using DG.Tweening;

public class AudioManager : BaseAutoSingleton<AudioManager>
{
    AudioSource source;
    string currentBgmKey;

    // 过渡的时间 不需要过度直接设置为 0
    readonly float duration = 1f;

    // 缓存加载好的混音器
    [SerializeField]
    AudioMixer mainMixer;

    // 记录设置的音量
    float userVolumeBGM = 0f;

    // 记录当前的淡入淡出动画，防止动画“打架”
    Tween fadeTween;

    // 初始化 AudioMixer
    public void InitMixer(string mixerKey, System.Action onComplete = null)
    {
        if (mainMixer != null)
        {
            onComplete?.Invoke();
            return;
        }

        AddressablesManager.Instance.LoadAssetAsync<AudioMixer>(mixerKey, (obj) =>
        {
            mainMixer = obj.Result;
            Debug.Log("混音器加载完成");
            onComplete?.Invoke();
        });
    }

    #region BGM

        // 统一处理 BGM 的音量渐变
        void DoMixerFade(float targetDb, System.Action onComplete = null)
        {
            // 如果有正在执行的渐变，先杀掉它，防止疯狂点击导致的数值乱跳
            fadeTween?.Kill();

            // 获取当前 Mixer 真实音量作为起点
            mainMixer.GetFloat("VolumeBGM", out float currentVol);

            // 使用 DOTween.To 驱动 float 数值变化
            fadeTween = DOTween.To
            (
                // 初始值
                () => currentVol,            
                x => 
                { 
                    // 赋值
                    currentVol = x; 
                    mainMixer.SetFloat("VolumeBGM", x); 
                },                           
                targetDb, // 目标值
                duration  // 持续时间
            ).OnComplete(() => 
            {
                // 渐变结束后执行回调（比如 Stop 或 Pause）
                onComplete?.Invoke();        
                fadeTween = null;
            });
        }

        // BGM 播放 
        public void PlayBGM(string name)
        {
            // 初始化 AudioSource
            if (source == null)
            {
                source = gameObject.AddComponent<AudioSource>();
                if (mainMixer != null)
                {
                    AudioMixerGroup[] groups = mainMixer.FindMatchingGroups("BGM");
                    if (groups.Length > 0) source.outputAudioMixerGroup = groups[0];
                }
                else Debug.Log("请创建一个 AudioMixer!");
            }

            if (currentBgmKey == name)
            {
                // 从暂停状态恢复播放
                if (!source.isPlaying)
                {
                    source.Play();
                    DoMixerFade(userVolumeBGM); 
                }
            }
            else
            {
                // 切换 BGM：不再使用协程，直接走内部的回调逻辑
                SwitchBGM(name);
            }
        }

        // 切换 BGM
        void SwitchBGM(string name)
        {
            // 先淡出旧音乐
            DoMixerFade(-80f, () => 
            {
                // 淡出结束后：停止播放、释放旧资源
                if (source != null && source.isPlaying) source.Stop();

                if (!string.IsNullOrEmpty(currentBgmKey))
                {
                    AddressablesManager.Instance.Release<AudioClip>(currentBgmKey);
                }

                // 异步加载新歌
                AddressablesManager.Instance.LoadAssetAsync<AudioClip>(name, (obj) =>
                {
                    currentBgmKey = name;
                    source.clip = obj.Result;
                    source.Play();

                    // 加载完毕且开始播放后：淡入到玩家设定的音量
                    DoMixerFade(userVolumeBGM);
                });
            });
        }

        // BGM 终止
        public void StopBGM()
        {
            if (source != null && source.isPlaying)
            {
                // 先淡出到静音，完成后再彻底 Stop
                DoMixerFade(-80f, () => source.Stop());
            }
        }

        // BGM 暂停
        public void PauseBGM()
        {
            if (source != null && source.isPlaying)
            {
                // 先淡出到静音，完成后再 Pause
                DoMixerFade(-80f, () => source.Pause());
            }
        }

        // BGM 音量控制
        public void ChangValueBGM(float value)
        {
            float clampedValue = Mathf.Clamp(value, 0.0001f, 1f);

            if (mainMixer != null)
            {
                float db = (clampedValue == 0.0001f) ? -80f : Mathf.Log10(clampedValue) * 40f;
                
                // 必须时刻记录玩家的设定意图，作为淡入的目标值
                userVolumeBGM = db;
                
                // 只有在没做渐变动画时，才实时调整。
                if (fadeTween == null)
                {
                    mainMixer.SetFloat("VolumeBGM", db);
                }
            }
            else Debug.Log("调用 ChangValueBGM 前得要先有个 AudioMixer!");
        }

    #endregion
}