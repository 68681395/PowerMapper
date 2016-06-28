﻿using System.Collections.Generic;
using Benchmarks.Generators;
using Benchmarks.Mapping;
using Benchmarks.Models;
using Benchmarks.ViewModels;
using Mapster;
using Nelibur.ObjectMapper;

namespace Benchmarks.Tests
{
    public class SimpleStructTest : BaseTest<List<Item>, List<ItemViewModel>>
    {
        private Wheatech.ObjectMapper.ObjectMapper _objectMapper;
        protected override List<Item> GetData()
        {
            return DataGenerator.GetItems(Count);
        }

        protected override void InitAutoMapper()
        {
            AutoMapperMapping.Init();
        }

        protected override void InitExpressMapper()
        {
            ExpressMapperMapping.Init();
        }

        protected override void InitOoMapper()
        {
            OoMapperMappings.Init();
        }

        protected override void InitValueInjectorMapper()
        {
            ValueInjectorMappings.Init();
        }

        protected override void InitMapsterMapper()
        {
            MapsterMapperMappings.Init();
        }

        protected override void InitTinyMapper()
        {
            TinyMapperMappings.Init();
        }

        protected override void InitNativeMapper()
        {
        }

        protected override void InitObjectMapper()
        {
            _objectMapper = ObjectMapperMapping.Init();
        }

        protected override List<ItemViewModel> AutoMapperMap(List<Item> src)
        {
            return AutoMapper.Mapper.Map<List<Item>, List<ItemViewModel>>(src);
        }

        protected override List<ItemViewModel> ExpressMapperMap(List<Item> src)
        {
            return ExpressMapper.Mapper.Map<List<Item>, List<ItemViewModel>>(src);
        }

        protected override List<ItemViewModel> OoMapperMap(List<Item> src)
        {
            return OoMapper.Mapper.Map<List<Item>, List<ItemViewModel>>(src);
        }

        protected override List<ItemViewModel> ValueInjectorMap(List<Item> src)
        {
            var list = new List<ItemViewModel>();
            foreach (var item in src)
            {
                list.Add(Omu.ValueInjecter.Mapper.Map<Item, ItemViewModel>(item));
            }
            return list;
        }

        protected override List<ItemViewModel> MapsterMap(List<Item> src)
        {
            return TypeAdapter.Adapt<List<Item>, List<ItemViewModel>>(src);
        }

        protected override List<ItemViewModel> TinyMapperMap(List<Item> src)
        {
            var list = new List<ItemViewModel>();
            foreach (var item in src)
            {
                list.Add(TinyMapper.Map<Item, ItemViewModel>(item));
            }
            return list;
        }

        protected override List<ItemViewModel> NativeMapperMap(List<Item> src)
        {
            var result = new List<ItemViewModel>();
            foreach (var item in src)
            {
                result.Add(NativeMapping.Map(item));
            }
            return result;
        }

        protected override List<ItemViewModel> ObjectMapperMap(List<Item> src)
        {
            return _objectMapper.Map<Item, ItemViewModel>(src);
        }

        protected override string TestName
        {
            get { return "SimpleStructTest"; }
        }

        protected override string Size
        {
            get { return "XS"; }
        }
    }
}
