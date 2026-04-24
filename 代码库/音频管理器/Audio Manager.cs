using UnityEngine;
using UnityEngine.Audio;
using DG.Tweening;
using System.Collections;

public class AudioManager : BaseAutoSingleton<AudioManager>
{
    AudioSource source;
    string currentBgmKey;

    [Header("BGM 淡入淡出")]
    // 淡入时间
    [SerializeField]
    float fadeInDuration = 0f;

    // 淡出时间
    [SerializeField]
    float fadeOutDuration = 0.8f;

    // 淡入缓动
    [SerializeField]
    Ease fadeInEase = Ease.OutSine;

    // 淡出缓动
    [SerializeField]
    Ease fadeOutEase = Ease.InSine;

    // 缓存加载好的混音器
    [SerializeField]
    AudioMixer mainMixer;

    // 记录设置的音量
    float userVolumeBGM = 0f;

    // 记录当前的淡入淡出动画，防止动画“打架”
    Tween fadeTween;

    // 当前是否正在切歌
    bool isSwitchingBGM;

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

    // 主音量控制
    public void ChangValueMain(float value)
    {
        float clampedValue = Mathf.Clamp(value, 0.0001f, 1f);

        if (mainMixer != null)
        {
            float db = (clampedValue == 0.0001f) ? -80f : Mathf.Log10(clampedValue) * 40f;
            mainMixer.SetFloat("VolumeMain", db);
        }
        else
        {
            Debug.Log("调用 ChangValueMain 前得要先有个 AudioMixer!");
        }
    }

    #region BGM

        // 统一处理 BGM 的音量渐变
        // isFadeIn = true 时使用淡入参数
        // isFadeIn = false 时使用淡出参数
        void DoMixerFade(float targetDb, bool isFadeIn, System.Action onComplete = null)
        {
            // 如果有正在执行的渐变，先杀掉它，防止疯狂点击导致的数值乱跳
            fadeTween?.Kill();

            if (mainMixer == null)
            {
                onComplete?.Invoke();
                return;
            }

            // 获取当前 Mixer 真实音量作为起点
            mainMixer.GetFloat("VolumeBGM", out float currentDb);

            // dB 转线性音量
            // 因为 dB 直接插值前面会很久没声音，后面突然爆出来
            float currentLinear = Mathf.Pow(10f, currentDb / 20f);
            float targetLinear = Mathf.Pow(10f, targetDb / 20f);

            // 根据是淡入还是淡出选择参数
            float duration = isFadeIn ? fadeInDuration : fadeOutDuration;
            Ease ease = isFadeIn ? fadeInEase : fadeOutEase;

            // 使用 DOTween.To 驱动 float 数值变化
            fadeTween = DOTween.To
            (
                // 初始值
                () => currentLinear,
                x =>
                {
                    // 赋值
                    currentLinear = x;

                    // 线性音量再转回 dB
                    float db = x <= 0.0001f ? -80f : Mathf.Log10(x) * 20f;

                    mainMixer.SetFloat("VolumeBGM", db);
                },
                targetLinear, // 目标值
                duration      // 持续时间
            )
            .SetEase(ease)
            .OnComplete(() =>
            {
                // 渐变结束后执行回调（比如 Stop 或 Pause）
                onComplete?.Invoke();
                fadeTween = null;
            });
        }

        // BGM 播放
        public void PlayBGM(string name, bool isloop = true)
        {
            // 防止切歌过程中重复调用
            if (isSwitchingBGM)
            {
                return;
            }

            // 初始化 AudioSource
            if (source == null)
            {
                source = gameObject.AddComponent<AudioSource>();

                if (mainMixer != null)
                {
                    AudioMixerGroup[] groups = mainMixer.FindMatchingGroups("BGM");

                    if (groups.Length > 0)
                    {
                        source.outputAudioMixerGroup = groups[0];
                    }
                }
                else
                {
                    Debug.Log("请创建一个 AudioMixer!");
                }
            }

            source.loop = isloop;

            // 如果当前就是这首歌
            if (currentBgmKey == name)
            {
                // 从暂停状态恢复播放
                if (!source.isPlaying)
                {
                    source.Play();

                    // 淡入到玩家设定的音量
                    DoMixerFade(userVolumeBGM, true);
                }
            }
            else
            {
                // 切换 BGM
                SwitchBGM(name);
            }
        }

        // 切换 BGM
        void SwitchBGM(string name)
        {
            isSwitchingBGM = true;

            // 先淡出旧音乐
            DoMixerFade(-80f, false, () =>
            {
                // 淡出结束后：停止播放、释放旧资源
                if (source != null && source.isPlaying)
                {
                    source.Stop();
                }

                if (!string.IsNullOrEmpty(currentBgmKey))
                {
                    AddressablesManager.Instance.Release<AudioClip>(currentBgmKey);
                }

                // 异步加载新歌
                AddressablesManager.Instance.LoadAssetAsync<AudioClip>(name, (obj) =>
                {
                    // 防止 AudioSource 被销毁
                    if (source == null)
                    {
                        isSwitchingBGM = false;
                        return;
                    }

                    currentBgmKey = name;

                    source.clip = obj.Result;
                    source.Play();

                    // 加载完毕且开始播放后：淡入到玩家设定的音量
                    DoMixerFade(userVolumeBGM, true);

                    isSwitchingBGM = false;
                });
            });
        }

        // BGM 终止
        public void StopBGM()
        {
            if (source != null && source.isPlaying)
            {
                // 先淡出到静音，完成后再彻底 Stop
                DoMixerFade(-80f, false, () =>
                {
                    if (source != null)
                    {
                        source.Stop();
                    }
                });
            }
        }

        // BGM 暂停
        public void PauseBGM()
        {
            if (source != null && source.isPlaying)
            {
                // 先淡出到静音，完成后再 Pause
                DoMixerFade(-80f, false, () =>
                {
                    if (source != null)
                    {
                        source.Pause();
                    }
                });
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

                // 只有在没做渐变动画时，才实时调整
                if (fadeTween == null)
                {
                    mainMixer.SetFloat("VolumeBGM", db);
                }
            }
            else
            {
                Debug.Log("调用 ChangValueBGM 前得要先有个 AudioMixer!");
            }
        }

    #endregion

    #region FX

        // FX 播放
        public void PlayFX(string name, AudioSource sourceFX)
        {
            // 防止外面传进来的是空
            if (sourceFX == null)
            {
                return;
            }

            // 初始化 AudioSource
            if (mainMixer != null)
            {
                AudioMixerGroup[] groups = mainMixer.FindMatchingGroups("FX");

                if (groups.Length > 0)
                {
                    sourceFX.outputAudioMixerGroup = groups[0];
                }
            }
            else
            {
                Debug.Log("请创建一个 AudioMixer!");
            }

            // 异步加载 FX
            AddressablesManager.Instance.LoadAssetAsync<AudioClip>(name, (obj) =>
            {
                // 防止加载完音效但是播放者消失了 比如敌人死亡
                if (sourceFX == null)
                {
                    return;
                }

                if (!sourceFX.gameObject.activeInHierarchy)
                {
                    return;
                }

                if (!sourceFX.enabled)
                {
                    return;
                }

                sourceFX.clip = obj.Result;
                sourceFX.Play();

                // 启动释放协程
                StartCoroutine(ReleaseWhenFinish(sourceFX, name));
            });
        }

        // FX 释放
        IEnumerator ReleaseWhenFinish(AudioSource source, string key)
        {
            yield return new WaitWhile
            (
                () =>
                source != null &&
                source.gameObject != null &&
                source.isPlaying
            );

            AddressablesManager.Instance.Release<AudioClip>(key);
        }

        // 防止异步加载出 bug
        // 没用对象池
        // 频繁销毁影响性能
        // 尽量少用临时 FX
        // 在指定位置播放临时 FX
        public void PlayFXAtPosition(string name, Vector3 position)
        {
            // 创建临时物体
            GameObject tempObj = new GameObject($"TempAudio_{name}");
            tempObj.transform.position = position;

            // 添加 AudioSource
            AudioSource tempSource = tempObj.AddComponent<AudioSource>();

            // 音效设置
            tempSource.spatialBlend = 0f; // 0 = 2D，1 = 3D
            tempSource.playOnAwake = false;

            // 挂载 Mixer Group
            if (mainMixer != null)
            {
                AudioMixerGroup[] groups = mainMixer.FindMatchingGroups("FX");

                if (groups.Length > 0)
                {
                    tempSource.outputAudioMixerGroup = groups[0];
                }
            }
            else
            {
                Debug.LogWarning("请创建一个 AudioMixer!");
            }

            // 异步加载 FX
            AddressablesManager.Instance.LoadAssetAsync<AudioClip>(name, (obj) =>
            {
                if (tempSource == null)
                {
                    return;
                }

                AudioClip clip = obj.Result;

                tempSource.clip = clip;
                tempSource.Play();

                // 播放完后自动释放资源并销毁对象
                StartCoroutine(ReleaseTempFX(tempSource, tempObj, name));
            });
        }

        // 临时 FX 回收
        IEnumerator ReleaseTempFX(AudioSource source, GameObject tempObj, string key)
        {
            yield return new WaitWhile
            (
                () =>
                source != null &&
                source.gameObject != null &&
                source.isPlaying
            );

            AddressablesManager.Instance.Release<AudioClip>(key);

            if (tempObj != null)
            {
                Destroy(tempObj);
            }
        }

        // FX 音量控制
        public void ChangValueFX(float value)
        {
            float clampedValue = Mathf.Clamp(value, 0.0001f, 1f);

            if (mainMixer != null)
            {
                float db = (clampedValue == 0.0001f) ? -80f : Mathf.Log10(clampedValue) * 40f;
                mainMixer.SetFloat("VolumeFX", db);
            }
            else
            {
                Debug.Log("调用 ChangValueFX 前得要先有个 AudioMixer!");
            }
        }

    #endregion
}