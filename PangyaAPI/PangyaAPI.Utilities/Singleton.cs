using System;
namespace PangyaAPI.Utilities
{
    public class Singleton<_ST> where _ST : class
    {
        public static _ST myInstance = null!;

        public static _ST getInstance()
        {
            if (myInstance == null)
                myInstance = Activator.CreateInstance<_ST>()
                    ?? throw new InvalidOperationException($"Unable to create singleton instance of {typeof(_ST).FullName}.");

            return myInstance;
        }

        protected Singleton()
        {
        }
    }
}
