﻿using System;
using System.Reflection.Emit;

namespace PowerMapper
{
    internal class LambdaMemberMapper<TSource, TMember> : MemberMapper
    {
        private readonly Func<TSource, TMember> _expression;
        private FuncInvokerBuilder<TSource, TMember> _invokerBuilder;
        private Type _sourceType;

        public LambdaMemberMapper(MappingContainer container, MemberMapOptions options, MappingMember targetMember, Func<TSource, TMember> expression)
            : base(container, options, targetMember, null)
        {
            if (expression == null)
            {
                throw new ArgumentNullException(nameof(expression));
            }
            _expression = expression;
        }

        public override Type SourceType => _sourceType ?? (_sourceType = _expression.GetType().GetGenericArguments()[1]);

        public override void Compile(ModuleBuilder builder)
        {
            base.Compile(builder);
            if (_invokerBuilder == null)
            {
                _invokerBuilder = new FuncInvokerBuilder<TSource, TMember>(_expression);
                _invokerBuilder.Compile(builder);
            }
        }

        protected override void EmitSource(CompilationContext context)
        {
            context.LoadSource(LoadPurpose.Parameter);
            _invokerBuilder.Emit(context);
            context.CurrentType = SourceType;
        }
    }
}
