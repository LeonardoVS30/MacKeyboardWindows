using System;
using System.IO;
using System.Media;
using System.Windows;

namespace MacKeyboardWindows.Services
{
    public class SoundService
    {
        private readonly SoundPlayer _clickPlayer;
        private readonly SoundPlayer _modifierPlayer;

        // Propiedad para controlar si el sonido está activo
        public bool IsEnabled { get; set; } = true;

        public SoundService()
        {
            _clickPlayer = LoadSound("Click.wav");
            _modifierPlayer = LoadSound("Modifier.wav") ?? _clickPlayer;
        }

        private SoundPlayer LoadSound(string fileName)
        {
            try
            {
                var uri = new Uri($"pack://application:,,,/Sounds/{fileName}");
                var resourceStream = Application.GetResourceStream(uri);

                if (resourceStream != null)
                {
                    var player = new SoundPlayer(resourceStream.Stream);
                    player.Load();
                    return player;
                }
            }
            catch (Exception) { }
            return null;
        }

        public void PlayClick()
        {
            // Solo reproducir si está habilitado y el reproductor existe
            if (IsEnabled && _clickPlayer != null)
            {
                try { _clickPlayer.Play(); } catch { }
            }
        }

        public void PlayModifier()
        {
            // Solo reproducir si está habilitado y el reproductor existe
            if (IsEnabled && _modifierPlayer != null)
            {
                try { _modifierPlayer.Play(); } catch { }
            }
        }
    }
}