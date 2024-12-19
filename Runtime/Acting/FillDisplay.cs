using System;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#else
using NaughtyAttributes;
#endif
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Readymade.Machinery.Acting
{
    public class FillDisplay : MonoBehaviour
    {
        [BoxGroup("Description")]
        [SerializeField]
        private TMP_Text label;

        [BoxGroup("Description")]
        [SerializeField]
        private TMP_Text maxValue;

        [BoxGroup("Description")]
        [SerializeField]
        private TMP_Text minValue;

        [BoxGroup("Description")]
        [SerializeField]
        private TMP_Text currentValue;

        [BoxGroup("Description")]
        [SerializeField]
        private TMP_Text nextValue;

        [BoxGroup("Description")]
        [SerializeField]
        private Image icon;

        [BoxGroup("Bar")] [SerializeField] private BarMode mode;

        [BoxGroup("Bar")]
        [ShowIf(nameof(mode), BarMode.Simple)]
        [SerializeField]
        private Image fill;

        [BoxGroup("Bar")]
        [ShowIf(nameof(mode), BarMode.Fancy)]
        [SerializeField]
        private RectTransform delta;

        [BoxGroup("Bar")]
        [ShowIf(nameof(mode), BarMode.Fancy)]
        [SerializeField]
        private RectTransform bar;

        [BoxGroup("Bar")]
        [ShowIf(nameof(mode), BarMode.Fancy)]
        [SerializeField]
        private Direction direction;

        [BoxGroup("Bar")]
        [ShowIf(nameof(mode), BarMode.Fancy)]
        [SerializeField]
        private Origin origin;

        [BoxGroup("Animation")]
        [SerializeField]
        private CanvasGroup group;

        public Image Icon => icon;

        public TMP_Text NextValue => nextValue;

        public TMP_Text CurrentValue => currentValue;

        public TMP_Text MinValue => minValue;

        public TMP_Text MaxValue => maxValue;

        public TMP_Text Label => label;

        public void SetFill(float t, float dt = 0)
        {
            t = Mathf.Clamp01(t);

            if (mode == BarMode.Simple)
            {
                if (fill)
                {
                    fill.fillAmount = t;
                }
            }
            else if (mode == BarMode.Fancy)
            {
                dt = Mathf.Clamp01(dt);
                float tt = Mathf.Clamp01(t + dt);

                if (bar)
                {
                    bar.anchorMin = direction switch
                    {
                        Direction.Horizontal => origin switch
                        {
                            Origin.BottomLeft => new Vector2(0.0f, 0.0f),
                            Origin.TopRight => new Vector2(1.0f - t, 0.0f),
                            Origin.Center => new Vector2(0.5f - t * 0.5f, 0.0f),
                            _ => throw new ArgumentOutOfRangeException()
                        },
                        Direction.Vertical => origin switch
                        {
                            Origin.BottomLeft => new Vector2(0.0f, 0.0f),
                            Origin.TopRight => new Vector2(0.0f, 1.0f - t),
                            Origin.Center => new Vector2(0.0f, 0.5f - t * 0.5f),
                            _ => throw new ArgumentOutOfRangeException()
                        },
                        _ => throw new ArgumentOutOfRangeException()
                    };
                    bar.anchorMax = direction switch
                    {
                        Direction.Horizontal => origin switch
                        {
                            Origin.BottomLeft => new Vector2(t, 1f),
                            Origin.TopRight => new Vector2(1.0f, 1.0f),
                            Origin.Center => new Vector2(0.5f + t * 0.5f, 1f),
                            _ => throw new ArgumentOutOfRangeException()
                        },
                        Direction.Vertical => origin switch
                        {
                            Origin.BottomLeft => new Vector2(1.0f, t),
                            Origin.TopRight => new Vector2(1.0f, 1.0f),
                            Origin.Center => new Vector2(1f, 0.5f + t * 0.5f),
                            _ => throw new ArgumentOutOfRangeException()
                        },
                        _ => throw new ArgumentOutOfRangeException()
                    };
                }

                if (delta)
                {
                    float min = Mathf.Min(t, tt);
                    float max = Mathf.Min(t, tt);
                    delta.anchorMin = direction switch
                    {
                        Direction.Horizontal => origin switch
                        {
                            Origin.BottomLeft => new Vector2(min, 0.0f),
                            Origin.TopRight => new Vector2(1f - max, 0.0f),
                            Origin.Center => new Vector2(0.5f - (max - min), 0.0f),
                            _ => throw new ArgumentOutOfRangeException()
                        },
                        Direction.Vertical => origin switch
                        {
                            Origin.BottomLeft => new Vector2(0.0f, min),
                            Origin.TopRight => new Vector2(0.0f, 1.0f - max),
                            Origin.Center => new Vector2(0.0f, 0.5f - (max - min)),
                            _ => throw new ArgumentOutOfRangeException()
                        },
                        _ => throw new ArgumentOutOfRangeException()
                    };
                    delta.anchorMax = direction switch
                    {
                        Direction.Horizontal => origin switch
                        {
                            Origin.BottomLeft => new Vector2(max, 1.0f),
                            Origin.TopRight => new Vector2(1.0f - min, 1.0f),
                            Origin.Center => new Vector2(0.5f + (max - min), 1.0f),
                            _ => throw new ArgumentOutOfRangeException()
                        },
                        Direction.Vertical => origin switch
                        {
                            Origin.BottomLeft => new Vector2(1.0f, max),
                            Origin.TopRight => new Vector2(1.0f, 1.0f - min),
                            Origin.Center => new Vector2(1f, 0.5f + (max - min)),
                            _ => throw new ArgumentOutOfRangeException()
                        },
                        _ => throw new ArgumentOutOfRangeException()
                    };
                }
            }
        }

        internal enum BarMode
        {
            Simple,
            Fancy
        }

        internal enum Direction
        {
            Horizontal,
            Vertical,
            Both
        }

        internal enum Origin
        {
            BottomLeft,
            TopRight,
            Center
        }
    }
}