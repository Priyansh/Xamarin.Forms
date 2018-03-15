﻿using System;
using System.ComponentModel;
using Android.Content.Res;
using Android.Graphics.Drawables;
using AView = Android.Views.View;
using Android.OS;
using Xamarin.Forms.PlatformConfiguration.AndroidSpecific;
using Specifics = Xamarin.Forms.PlatformConfiguration.AndroidSpecific;
using AButton = Android.Widget.Button;
using AColor = Android.Graphics.Color;

namespace Xamarin.Forms.Platform.Android
{
	internal class BorderBackgroundManager : IDisposable
	{
		Drawable _defaultDrawable;
		BorderDrawable _backgroundDrawable;
		RippleDrawable _rippleDrawable;
		bool _drawableEnabled;
		bool _disposed;
		IBorderVisualElementRenderer _renderer;
		VisualElement Element => _renderer?.Element;
		AView Control => _renderer?.View;

		public BorderBackgroundManager(IBorderVisualElementRenderer renderer)
		{
			_renderer = renderer;
			_renderer.ElementChanged += OnElementChanged;
		}

		void OnElementChanged(object sender, VisualElementChangedEventArgs e)
		{
			if (e.OldElement != null)
			{
				(e.OldElement as IBorderController).PropertyChanged -= BorderElementPropertyChanged;
			}

			if (e.NewElement != null)
			{
				if (BorderElement != null)
				{
					BorderElement.PropertyChanged -= BorderElementPropertyChanged;
				}
				BorderElement = (IBorderController)e.NewElement;
				BorderElement.PropertyChanged += BorderElementPropertyChanged;
			}

			Reset();
			UpdateDrawable();
		}


		public IBorderController BorderElement
		{
			get;
			private set;
		}

		public void UpdateDrawable()
		{
			if (BorderElement == null || Control == null)
				return;

			bool cornerRadiusIsDefault = !BorderElement.IsSet(BorderElement.CornerRadiusProperty) || (BorderElement.CornerRadius == (int)BorderElement.CornerRadiusProperty.DefaultValue || BorderElement.CornerRadius == BorderDrawable.DefaultCornerRadius);
			bool backgroundColorIsDefault = !BorderElement.IsSet(VisualElement.BackgroundColorProperty) || BorderElement.BackgroundColor == (Color)VisualElement.BackgroundColorProperty.DefaultValue;
			bool borderColorIsDefault = !BorderElement.IsSet(BorderElement.BorderColorProperty) || BorderElement.BorderColor == (Color)BorderElement.BorderColorProperty.DefaultValue;
			bool borderWidthIsDefault = !BorderElement.IsSet(BorderElement.BorderWidthProperty) || BorderElement.BorderWidth == (double)BorderElement.BorderWidthProperty.DefaultValue;

			if (backgroundColorIsDefault
				&& cornerRadiusIsDefault
				&& borderColorIsDefault
				&& borderWidthIsDefault)
			{
				if (!_drawableEnabled)
					return;

				if (_defaultDrawable != null)
					Control.SetBackground(_defaultDrawable);

				_drawableEnabled = false;
			}
			else
			{
				if (_backgroundDrawable == null)
					_backgroundDrawable = new BorderDrawable(Control.Context.ToPixels, Forms.GetColorButtonNormal(Control.Context));

				_backgroundDrawable.Button = BorderElement;

				var useDefaultPadding = _renderer.UseDefaultPadding();

				int paddingTop = useDefaultPadding ? Control.PaddingTop : 0;
				int paddingLeft = useDefaultPadding ? Control.PaddingLeft : 0;

				var useDefaultShadow = _renderer.UseDefaultShadow();

				float shadowRadius = useDefaultShadow ? 2 : _renderer.ShadowRadius;
				float shadowDx = useDefaultShadow ? 0 : _renderer.ShadowDx;
				float shadowDy = useDefaultShadow ? 4 : _renderer.ShadowDy;
				AColor shadowColor = useDefaultShadow ? _backgroundDrawable.PressedBackgroundColor.ToAndroid() : _renderer.ShadowColor;

				_backgroundDrawable.SetPadding(paddingTop, paddingLeft);

				if (_renderer.IsShadowEnabled())
				{
					_backgroundDrawable.SetShadow(shadowDy, shadowDx, shadowColor, shadowRadius);
				}

				if (_drawableEnabled)
					return;

				if (_defaultDrawable == null)
					_defaultDrawable = Control.Background;

				if (Forms.IsLollipopOrNewer)
				{
					var rippleColor = _backgroundDrawable.PressedBackgroundColor.ToAndroid();

					_rippleDrawable = new RippleDrawable(ColorStateList.ValueOf(rippleColor), _backgroundDrawable, null);
					Control.SetBackground(_rippleDrawable);
				}
				else
				{
					Control.SetBackground(_backgroundDrawable);
				}

				_drawableEnabled = true;
			}

			Control.Invalidate();
		}

		public void Reset()
		{
			if (_drawableEnabled)
			{
				_drawableEnabled = false;
				_backgroundDrawable?.Reset();
				_backgroundDrawable = null;
				_rippleDrawable = null;
			}
		}

		public void UpdateBackgroundColor()
		{
			UpdateDrawable();
		}

		public void Dispose()
		{
			Dispose(true);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!_disposed)
			{
				if (disposing)
				{
					_backgroundDrawable?.Dispose();
					_backgroundDrawable = null;
					_defaultDrawable?.Dispose();
					_defaultDrawable = null;
					_rippleDrawable?.Dispose();
					_rippleDrawable = null;
					if (BorderElement != null)
					{
						BorderElement.PropertyChanged -= BorderElementPropertyChanged;
						BorderElement = null;
					}

					if (_renderer != null)
					{
						_renderer.ElementChanged -= OnElementChanged;
						_renderer = null;
					}
				}
				_disposed = true;
			}
		}

		void BorderElementPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName.Equals(BorderElement.BorderColorProperty.PropertyName) ||
				e.PropertyName.Equals(BorderElement.BorderWidthProperty.PropertyName) ||
				e.PropertyName.Equals(BorderElement.CornerRadiusProperty.PropertyName) ||
				e.PropertyName.Equals(VisualElement.BackgroundColorProperty.PropertyName) ||
				e.PropertyName.Equals(Specifics.Button.UseDefaultPaddingProperty.PropertyName) ||
				e.PropertyName.Equals(Specifics.Button.UseDefaultShadowProperty.PropertyName))
			{
				Reset();
				UpdateDrawable();
			}
		}

	}
}