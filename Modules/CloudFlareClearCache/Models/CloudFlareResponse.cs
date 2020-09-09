/*
MIT License

Copyright (c) Upendo Ventures, LLC

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace Upendo.Modules.CloudFlareClearCache.Models
{
    [DataContract]
    [Serializable]
    public class CloudFlareResponse : ICloudFlareResponse
    {
        [DataMember(Name = "result")]
        [JsonProperty(PropertyName = "result")]
        public CloudFlareResult Result { get; set; }

        [DataMember(Name = "success")]
        [JsonProperty(PropertyName = "success")]
        public bool Success { get; set; }

        [DataMember(Name = "errors")]
        [JsonProperty(PropertyName = "errors")]
        public List<CloudFlareError> Errors { get; set; }

        [DataMember(Name = "messages")]
        [JsonProperty(PropertyName = "messages")]
        public List<string> Messages { get; set; }
    }

    [DataContract]
    [Serializable]
    public class CloudFlareResult : ICloudFlareResult
    {
        [DataMember(Name = "id")]
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }
    }

    [DataContract]
    [Serializable]
    public class CloudFlareError : ICloudFlareError
    {
        [DataMember(Name = "code")]
        [JsonProperty(PropertyName = "code")]
        public int Code { get; set; }

        [DataMember(Name = "message")]
        [JsonProperty(PropertyName = "message")]
        public string Message { get; set; }
    }
}