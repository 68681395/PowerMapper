﻿using System;
using System.Collections.Concurrent;

namespace Wheatech.EmitMapper
{
    internal class InstanceMapper<TSource, TTarget> : IInstanceMapper<TSource, TTarget>
    {
        private readonly Func<TSource, TTarget> _converter;
        private readonly Action<TSource, TTarget> _mapper;

        private static readonly ConcurrentDictionary<ObjectMapper, InstanceMapper<TSource, TTarget>> _mappers =
            new ConcurrentDictionary<ObjectMapper, InstanceMapper<TSource, TTarget>>();

        private InstanceMapper(ObjectMapper container)
        {
            _converter = container.GetMapFunc<TSource, TTarget>();
            _mapper = container.GetMapAction<TSource, TTarget>();
        }

        public Func<TSource, TTarget> Converter => _converter;

        public Action<TSource, TTarget> Mapper => _mapper;

        public static InstanceMapper<TSource, TTarget> GetInstance(ObjectMapper container)
        {
            return _mappers.GetOrAdd(container, key => new InstanceMapper<TSource, TTarget>(key));
        }

        public TTarget Map(TSource source)
        {
            if (ReferenceEquals(source, null))
            {
                return default(TTarget);
            }
            return _converter(source);
        }

        public void Map(TSource source, TTarget target)
        {
            if (!ReferenceEquals(source, null) && !ReferenceEquals(target, null))
            {
                _mapper(source, target);
            }
        }
    }
}
