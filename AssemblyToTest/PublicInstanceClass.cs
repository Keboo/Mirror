namespace AssemblyToTest
{
    public class PublicInstanceClass
    {
        private void PrivateMethod()
        {
            MethodInvocation.RegisterCall<PublicInstanceClass>();
        }

        private protected void PrivateProtectedMethod()
        {
            MethodInvocation.RegisterCall<PublicInstanceClass>();
        }

        private protected void ProtectedMethod()
        {
            MethodInvocation.RegisterCall<PublicInstanceClass>();
        }

        internal void InternalMethod()
        {
            MethodInvocation.RegisterCall<PublicInstanceClass>();
        }

        protected internal void ProtectedInternalMethod()
        {
            MethodInvocation.RegisterCall<PublicInstanceClass>();
        }

        public void PublicMethod()
        {
            MethodInvocation.RegisterCall<PublicInstanceClass>();
        }
    }
}