using Microsoft.AspNetCore.Http;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Nachonet.FileManager.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nachonet.FileManager.Test
{
    [TestClass]
    public class ByteRangeTest
    {
        [TestMethod]
        public void TestRange5To()
        {
            var request = new MockHttpRequest("https://localhost");
            request.Headers.Range = "5-";
            var range = ByteRange.FromRequest(request);
            Assert.IsTrue(range.RangeSpecified);
            Assert.AreEqual(5, range.Start);
            Assert.IsNull(range.End);
        }

        [TestMethod]
        public void TestRangeAnyTo5()
        {
            var request = new MockHttpRequest("https://localhost");
            request.Headers.Range = "-5";
            var range = ByteRange.FromRequest(request);
            Assert.IsTrue(range.RangeSpecified);
            Assert.IsNull(range.Start);
            Assert.AreEqual(5, range.End);
        }

        [TestMethod]
        public void TestRange5To100()
        {
            var request = new MockHttpRequest("https://localhost");
            request.Headers.Range = "5-100";
            var range = ByteRange.FromRequest(request);
            Assert.IsTrue(range.RangeSpecified);
            Assert.AreEqual(5, range.Start);
            Assert.AreEqual(100, range.End);
        }

        public class MockHttpRequest : HttpRequest
        {
            public MockHttpRequest(string uriString)
            {
                Headers = new HeaderDictionary();
                var uri = new Uri(uriString);
                Method = "GET";
                Scheme = uri.Scheme;
                Protocol = uri.Scheme;
                Host = new HostString(uri.Host);
                PathBase = new PathString(uri.LocalPath);
                Query = new QueryCollection(0);
                IsHttps = string.Equals(uri.Scheme, "https", StringComparison.CurrentCultureIgnoreCase);
                Body = new MemoryStream();
            }

            public override HttpContext HttpContext => throw new NotImplementedException();

            public override string Method { get; set; }
            public override string Scheme { get; set; }
            public override bool IsHttps { get; set; }
            public override HostString Host { get; set; }
            public override PathString PathBase { get; set; }
            public override PathString Path { get; set; }
            public override QueryString QueryString { get; set; }
            public override IQueryCollection Query { get; set; }
            public override string Protocol { get; set; }

            public override IHeaderDictionary Headers { get; }

            public override IRequestCookieCollection Cookies { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
            public override long? ContentLength { get; set; }
            public override string? ContentType { get; set; }
            public override Stream Body { get; set; }

            public override bool HasFormContentType => throw new NotImplementedException();

            public override IFormCollection Form { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

            public override Task<IFormCollection> ReadFormAsync(CancellationToken cancellationToken = default)
            {
                throw new NotImplementedException();
            }
        }
    }
}
