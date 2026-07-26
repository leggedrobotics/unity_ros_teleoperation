namespace RSL.Core
{
    static class Utils
    {
        public static void writeToByteArray(this float value, byte[] array, int startIndex)
        {
            int raw = System.Runtime.CompilerServices.Unsafe.As<float, int>(ref value);

            array[startIndex + 0] = (byte)(raw);
            array[startIndex + 1] = (byte)(raw >> 8);
            array[startIndex + 2] = (byte)(raw >> 16);
            array[startIndex + 3] = (byte)(raw >> 24);
        }

        public static void writeToByteArray(this uint value, byte[] array, int startIndex)
        {
            array[startIndex + 0] = (byte)(value);
            array[startIndex + 1] = (byte)(value >> 8);
            array[startIndex + 2] = (byte)(value >> 16);
            array[startIndex + 3] = (byte)(value >> 24);
        }
    }
}