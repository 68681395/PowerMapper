﻿using System;

namespace Wheatech.ObjectMapper.UnitTests.BusinessModel
{
    public class OrderItem
    {
        public Guid OrderItemId { get; set; }

        public Guid ProductId { get; set; }

        public int Quantity { get; set; }
    }
}
