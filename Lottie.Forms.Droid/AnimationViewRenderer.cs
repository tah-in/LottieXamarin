using System;
using System.ComponentModel;
using Android.Views;
using Com.Airbnb.Lottie;
using Lottie.Forms;
using Lottie.Forms.Droid;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;
using System.Net.Http;
using Square.OkHttp3;
using Org.Json;
using System.IO;
using System.Text;

[assembly: ExportRenderer(typeof(AnimationView), typeof(AnimationViewRenderer))]

namespace Lottie.Forms.Droid
{
    public class AnimationViewRenderer : Xamarin.Forms.Platform.Android.AppCompat.ViewRenderer<AnimationView,
        LottieAnimationView>
    {
        private LottieAnimationView _animationView;
        private AnimatorListener _animatorListener;
        private OkHttpClient client;

        /// <summary>
        ///     Used for registration with dependency service
        /// </summary>
        public static void Init()
        {
            // needed because of this linker issue: https://bugzilla.xamarin.com/show_bug.cgi?id=31076
#pragma warning disable 0219
            var dummy = new AnimationViewRenderer();
#pragma warning restore 0219
        }

        protected override void OnElementChanged(ElementChangedEventArgs<AnimationView> e)
        {
            base.OnElementChanged(e);

            if (Control == null)
            {
                _animationView = new LottieAnimationView(Context);
                _animatorListener = new AnimatorListener(PlaybackFinished);
                _animationView.AddAnimatorListener(_animatorListener);
                SetNativeControl(_animationView);
            }

            if (e.OldElement != null)
            {
                e.OldElement.OnPlay -= OnPlay;
                e.OldElement.OnPause -= OnPause;
                _animationView.SetOnClickListener(null);
            }

            if (e.NewElement != null)
            {
                e.NewElement.OnPlay += OnPlay;
                e.NewElement.OnPause += OnPause;
                _animationView.Speed = e.NewElement.Speed;
                _animationView.Loop(e.NewElement.Loop);

                _animationView.SetOnClickListener(new ClickListener(e.NewElement));

                if (!string.IsNullOrEmpty(e.NewElement.Animation))
                {
                    //_animationView.SetAnimation(e.NewElement.Animation);
                    InitAnimationViewForElement(Element);
                    Element.Duration = TimeSpan.FromMilliseconds(_animationView.Duration);
                }

                if (e.NewElement.AutoPlay) _animationView.PlayAnimation();
            }
        }

        private void OnPlay(object sender, EventArgs e)
        {
            if (_animationView != null
                && _animationView.Handle != IntPtr.Zero)
            {
                if (_animationView.Progress > 0f)
                {
                    _animationView.ResumeAnimation();
                }
                else
                {
                    _animationView.PlayAnimation();
                }
                Element.IsPlaying = true;
            }
        }

        private void OnPause(object sender, EventArgs e)
        {
            if (_animationView != null
                && _animationView.Handle != IntPtr.Zero)
            {
                _animationView.PauseAnimation();
                Element.IsPlaying = false;
            }
        }

        private void PlaybackFinished()
        {
            Element.PlaybackFinished();
        }

        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == AnimationView.AnimationProperty.PropertyName)
            {
                //_animationView.SetAnimation(Element.Animation);
                InitAnimationViewForElement(Element);
                Element.Duration = TimeSpan.FromMilliseconds(_animationView.Duration);
                if (Element.AutoPlay) _animationView.PlayAnimation();
            }

            if (e.PropertyName == AnimationView.SpeedProperty.PropertyName)
                _animationView.Speed = Element.Speed;

            if (e.PropertyName == AnimationView.ProgressProperty.PropertyName)
            {
                _animationView.PauseAnimation();
                _animationView.Progress = Element.Progress;
            }

            if (e.PropertyName == AnimationView.LoopProperty.PropertyName) 
                _animationView?.Loop(Element.Loop);

            base.OnElementPropertyChanged(sender, e);
        }

        public class ClickListener : Java.Lang.Object, IOnClickListener
        {
            private readonly AnimationView _animationView;

            public ClickListener(AnimationView animationView)
            {
                _animationView = animationView;
            }

            public void OnClick(Android.Views.View v)
            {
                _animationView.Click();
            }
        }

        private void InitAnimationViewForElement(AnimationView theElement)
        { 
            if(!theElement.IsLocal)
            {
                LoadUrl(theElement.Animation);
            }
            else
            {
                if (File.Exists(theElement.Animation))
                {
                    var fileStream = new FileStream(theElement.Animation, FileMode.Open, FileAccess.Read);
                    using (var streamReader = new StreamReader(fileStream, Encoding.UTF8))
                    {
                        var json = new JSONObject(streamReader.ReadToEnd());
                        _animationView.SetAnimation(json);
                    }
                }
                else
                {
                    _animationView.SetAnimation(theElement.Animation);
                }
            }
        }
        private void LoadUrl(String url)
        {
            
            Request request;
            try
            {
                request = new Request.Builder()
                                     .Url(url)
                                     .Build();
            }
            catch (Exception ex)
            {
                //OnLoadError();
                return;
            }


            if (this.client == null)
                this.client = new OkHttpClient();

            client.NewCall(request).Enqueue((ICall call, Response response) =>
            {
                if (!response.IsSuccessful)
                {
                    //OnLoadError();
                }

                try
                {
                    LottieComposition.Factory.FromJsonString(response.Body().String(), (composition) =>
                    {
                        _animationView.Composition = composition;
                    });
                }
                catch
                {
                    //OnLoadError();
                }

            }, (ICall call, Java.IO.IOException e) => 
            {
                
                //OnLoadError();
            });
        }
    }
}