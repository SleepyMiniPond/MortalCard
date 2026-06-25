using System;
using System.Linq;

public static class OptionTestValue
{
    public static Type OptionOf(Type valueType)
    {
        return None(valueType).GetType();
    }

    public static object Some(Type valueType, object value)
    {
        var method = GetOptionFactoryMethod("Some", parameterCount: 1)
            .MakeGenericMethod(valueType);
        return method.Invoke(null, new[] { value });
    }

    public static object None(Type valueType)
    {
        var method = GetOptionFactoryMethod("None", parameterCount: 0)
            .MakeGenericMethod(valueType);
        return method.Invoke(null, Array.Empty<object>());
    }

    private static System.Reflection.MethodInfo GetOptionFactoryMethod(string name, int parameterCount)
    {
        var optionType = Type.GetType("Optional.Option, Optional");
        if (optionType == null)
        {
            throw new InvalidOperationException("找不到 Optional.Option 型別，請確認 Optional.dll 已被 Unity 載入。");
        }

        return optionType
            .GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Single(method =>
                method.Name == name &&
                method.IsGenericMethodDefinition &&
                method.ReturnType.GetGenericArguments().Length == 1 &&
                method.GetParameters().Length == parameterCount &&
                (parameterCount == 0 || method.GetParameters()[0].ParameterType.IsGenericParameter));
    }
}
