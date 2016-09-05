﻿using System;
#if Net35
using System.Collections.Generic;
#else
using System.Collections.Concurrent;
#endif
using System.Reflection;
using System.Reflection.Emit;

namespace PowerMapper
{
    internal class FromStringConverter : ValueConverter
    {
        private static readonly MethodInfo _enumParseMethod;
        private static readonly MethodInfo _checkEmptyMethod;
        private static readonly MethodInfo _stringTrimMethod;

        static FromStringConverter()
        {
#if NetCore
            _enumParseMethod = typeof(Enum).GetTypeInfo().GetMethod("Parse", BindingFlags.Public | BindingFlags.Static);
            _checkEmptyMethod = typeof(string).GetTypeInfo().GetMethod("IsNullOrWhiteSpace", BindingFlags.Public | BindingFlags.Static);
            _stringTrimMethod = typeof(string).GetTypeInfo().GetMethod("Trim", BindingFlags.Public | BindingFlags.Instance);
#else
            _enumParseMethod = typeof(Enum).GetMethod("Parse", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(Type), typeof(string) }, null);
            _checkEmptyMethod = typeof(string).GetMethod("IsNullOrWhiteSpace", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string) }, null);
            _stringTrimMethod = typeof(string).GetMethod("Trim", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
#endif
        }

#if Net35
        private static readonly Dictionary<Type, MethodInfo> _methods = new Dictionary<Type, MethodInfo>();

        private static MethodInfo GetConvertMethod(Type targetType)
        {
            MethodInfo method;
            if (!_methods.TryGetValue(targetType, out method))
            {
                lock (_methods)
                {
                    if (!_methods.TryGetValue(targetType, out method))
                    {
                        method = FindConvertMethod(targetType);
                        _methods.Add(targetType, method);
                    }
                }
            }
            return method;
        }
#else
        private static readonly ConcurrentDictionary<Type, MethodInfo> _methods = new ConcurrentDictionary<Type, MethodInfo>();

        private static MethodInfo GetConvertMethod(Type targetType)
        {
            return _methods.GetOrAdd(targetType, FindConvertMethod);
        }
#endif

        private static MethodInfo FindConvertMethod(Type type)
        {
#if NetCore
            var typeInfo = type.GetTypeInfo();
            return typeInfo.IsEnum ? _enumParseMethod : typeInfo.GetMethod("Parse", BindingFlags.Public | BindingFlags.Static);
#else
            return type.IsEnum ? _enumParseMethod : type.GetMethod("Parse", BindingFlags.Public | BindingFlags.Static, null, new[] {typeof(string)}, null);
#endif
        }

        public override int Match(ConverterMatchContext context)
        {
            if (context.SourceType == typeof(string))
            {
                var targetType = context.TargetType;
#if NetCore
                if (targetType.IsNullable() && GetConvertMethod(targetType.GetTypeInfo().GetGenericArguments()[0]) != null) return 1;
#else          
                if (targetType.IsNullable() && GetConvertMethod(targetType.GetGenericArguments()[0]) != null) return 1;
#endif
                if (GetConvertMethod(context.TargetType) != null) return 0;
            }
            return -1;
        }

        public override void Compile(ModuleBuilder builder)
        {
        }

        public override void Emit(Type sourceType, Type targetType, CompilationContext context)
        {
#if NetCore
            var reflectingTargetType = targetType.GetTypeInfo();
#else
            var reflectingTargetType = targetType;
#endif
            var target = context.DeclareLocal(targetType);
            var local = context.DeclareLocal(sourceType);
            context.Emit(OpCodes.Stloc, local);

            if (targetType.IsNullable())
            {
                var labelEnd = context.DefineLabel();
                var labelFirst = context.DefineLabel();
                // if(source == null)
                context.Emit(OpCodes.Ldloc, local);
                context.Emit(OpCodes.Brtrue, labelFirst);

                // target = null;
                context.Emit(OpCodes.Ldloca, target);
                context.Emit(OpCodes.Initobj, targetType);

                // goto end;
                context.Emit(OpCodes.Br, labelEnd);
                context.MakeLabel(labelFirst);

                // if(string.IsNullOrWhiteSpace(source))
                var labelSecond = context.DefineLabel();
                context.Emit(OpCodes.Ldloc, local);
                context.EmitCall(_checkEmptyMethod);
                context.Emit(OpCodes.Brfalse, labelSecond);

                // target = new Nullable<T>(default(T));
                context.EmitDefault(reflectingTargetType.GetGenericArguments()[0]);
                context.Emit(OpCodes.Newobj, reflectingTargetType.GetConstructors()[0]);
                context.Emit(OpCodes.Stloc, target);

                // goto end;
                context.Emit(OpCodes.Br, labelEnd);
                context.MakeLabel(labelSecond);

                // target = new Nullable<$EnumType$>(($EnumType$)Enum.Parse(typeof($EnumType$),source));
                // or
                // target = new Nullable<$TargetType$>($TargetType$.Parse(source));
                var underlingType = reflectingTargetType.GetGenericArguments()[0];
#if NetCore
                if (underlingType.GetTypeInfo().IsEnum)
#else
                if (underlingType.IsEnum)
#endif
                {
                    context.EmitTypeOf(underlingType);
                }
                context.Emit(OpCodes.Ldloc, local);
                context.EmitCall(_stringTrimMethod);
                context.EmitCall(GetConvertMethod(underlingType));
                context.EmitCast(underlingType);
                context.Emit(OpCodes.Newobj, reflectingTargetType.GetConstructors()[0]);
                context.Emit(OpCodes.Stloc, target);

                context.MakeLabel(labelEnd);
                context.Emit(OpCodes.Ldloc, target);
            }
            else
            {
                // if(!string.IsNullOrWhiteSpace(source))
                var label = context.DefineLabel();
                context.Emit(OpCodes.Ldloc, local);
                context.EmitCall(_checkEmptyMethod);
                context.Emit(OpCodes.Brtrue, label);

                // target = ($EnumType$)Enum.Parse(typeof($EnumType$),source);
                // or
                // target = $TargetType$.Parse(source);
                if (reflectingTargetType.IsEnum)
                {
                    context.EmitTypeOf(targetType);
                }
                context.Emit(OpCodes.Ldloc, local);
                context.EmitCall(_stringTrimMethod);
                context.EmitCall(GetConvertMethod(targetType));
                context.EmitCast(targetType);
                context.Emit(OpCodes.Stloc, target);

                // goto end;
                var labelEnd = context.DefineLabel();
                context.Emit(OpCodes.Br, labelEnd);

                // target = default(T);
                context.MakeLabel(label);
                context.EmitDefault(targetType);
                context.Emit(OpCodes.Stloc, target);

                context.MakeLabel(labelEnd);
                context.Emit(OpCodes.Ldloc, target);
            }
        }
    }
}
