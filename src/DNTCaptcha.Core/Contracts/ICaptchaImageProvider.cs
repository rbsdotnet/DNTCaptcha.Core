﻿namespace DNTCaptcha.Core.Contracts
{
    /// <summary>
    /// Captcha Image Provider
    /// </summary>
    public interface ICaptchaImageProvider
    {
        /// <summary>
        /// Creates the captcha image.
        /// </summary>
        byte[] DrawCaptcha(string message, string foreColor, string backColor, float fontSize, string fontName);

        /// <summary>
        /// Creates the captcha image with noise.
        /// </summary>
        byte[] DrawCaptcha(string message, float fontSize, string fontName);
    }
}