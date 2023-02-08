using System.Media;

namespace VL.Lib
{
    public static class SystemSound
    {
        public static void Asterisk(bool play)
        {
            if (play)
                SystemSounds.Asterisk.Play();
        }

        public static void Beep(bool play)
        {
            if (play)
                SystemSounds.Beep.Play();
        }

        public static void Exclamation(bool play)
        {
            if (play)
                SystemSounds.Exclamation.Play();
        }

        public static void Hand(bool play)
        {
            if (play)
                SystemSounds.Hand.Play();
        }

        public static void Question(bool play)
        {
            if (play)
                SystemSounds.Question.Play();
        }
    }
}
