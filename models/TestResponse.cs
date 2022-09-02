using System;

namespace ConvertToPdf.models
{
    public class TestResponse
    {
        public Guid Id { get; set; }
        public TestRequest TestData { get; set; }
    }
}