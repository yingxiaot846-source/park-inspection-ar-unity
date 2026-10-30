using System;

namespace MvpArSubmitter
{
    /// <summary>
    /// POST /api/issues 的请求体。
    /// JsonUtility 以 C# 字段名作为 JSON key，字段必须保持小写，严禁改成属性。
    /// </summary>
    [Serializable]
    public class CreateIssueRequest
    {
        public string title;
        public string description;
        public string priority;
        public Vector3Payload position;
    }

    /// <summary>
    /// 契约中的 position 结构：{"x":..,"y":..,"z":..}
    /// </summary>
    [Serializable]
    public class Vector3Payload
    {
        public float x;
        public float y;
        public float z;
    }

    /// <summary>
    /// 服务端返回的完整 Issue（201 / 200 时的响应体）。
    /// </summary>
    [Serializable]
    public class IssueResponse
    {
        public string id;
        public string title;
        public string description;
        public string priority;
        public string status;
        public Vector3Payload position;
        public string createdAt;
        public string updatedAt;
    }

    /// <summary>
    /// 错误信封：{"error":{"code":"...","message":"..."}}
    /// </summary>
    [Serializable]
    public class ErrorEnvelope
    {
        public ErrorBody error;
    }

    [Serializable]
    public class ErrorBody
    {
        public string code;
        public string message;
    }
}
