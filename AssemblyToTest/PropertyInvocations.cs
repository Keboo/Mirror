namespace AssemblyToTest
{
    public class PropertyInvocations
    {
        private int _PublicProperty;
        public int PublicProperty
        {
            get
            {
                MethodInvocation.RegisterGetter<PropertyInvocations>();
                return _PublicProperty;
            }
            set
            {
                MethodInvocation.RegisterSetter<PropertyInvocations>(value);
                _PublicProperty = value;
            }
        }

        private int _InternalProperty;
        internal int InternalProperty
        {
            get
            {
                MethodInvocation.RegisterGetter<PropertyInvocations>();
                return _InternalProperty;
            }
            set
            {
                MethodInvocation.RegisterSetter<PropertyInvocations>(value);
                _InternalProperty = value;
            }
        }

        private PropertyValue _PrivateProperty;
        private PropertyValue PrivateProperty
        {
            get
            {
                MethodInvocation.RegisterGetter<PropertyInvocations>();
                return _PrivateProperty;
            }
            set
            {
                MethodInvocation.RegisterSetter<PropertyInvocations>(value);
                _PrivateProperty = value;
            }
        }

        private PropertyValue PrivateReadonlyProperty { get; } = new PropertyValue { Value = 24 };

        private static int _PublicStaticProperty;
        public static int PublicStaticProperty
        {
            get
            {
                MethodInvocation.RegisterGetter<PropertyInvocations>();
                return _PublicStaticProperty;
            }
            set
            {
                MethodInvocation.RegisterSetter<PropertyInvocations>(value);
                _PublicStaticProperty = value;
            }
        }

        private static int _InternalStaticProperty;
        internal static int InternalStaticProperty
        {
            get
            {
                MethodInvocation.RegisterGetter<PropertyInvocations>();
                return _InternalStaticProperty;
            }
            set
            {
                MethodInvocation.RegisterSetter<PropertyInvocations>(value);
                _InternalStaticProperty = value;
            }
        }

        private static PropertyValue _PrivateStaticProperty;
        private static PropertyValue PrivateStaticProperty
        {
            get
            {
                MethodInvocation.RegisterGetter<PropertyInvocations>();
                return _PrivateStaticProperty;
            }
            set
            {
                MethodInvocation.RegisterSetter<PropertyInvocations>(value);
                _PrivateStaticProperty = value;
            }
        }
    }

    internal class PropertyValue
    {
        internal int Value { get; set; }
    }
}