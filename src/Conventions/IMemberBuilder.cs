﻿namespace Wheatech.ObjectMapper
{
    internal interface IMemberBuilder
    {
        void EmitGetter(CompilationContext context);

        void EmitSetter(CompilationContext context);
    }
}
