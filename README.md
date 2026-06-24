# Copilot.Byok.OpenAi
让 Visual Studio Copilot 使用第三方兼容 OpenAI API 的模型。

功能列表：
1. 模型分组：组名做为 Copilot 的模型 ID，同种模型不同 API Key 可以分为多组
2. 负载均衡：每组模型可以配置多个 API Key，按轮询方式分配请求
3. 并发控制：每个模型可以配置最大并发请求数，超过限制的请求会被排队等待
4. 速率限制：每个模型可以配置每分钟最大请求数，超过限制的请求会被等待延迟处理


### 运行 Copilot.Byok.OpenAi
直接运行程序后，它会监听 443 端口，并自动将颁发的 CA 证书安装到受信任的根证书颁发机构中。

你可以使用以下命令将 Copilot.Byok.OpenAi 安装为 Windows 服务：
```
./Copilot.Byok.OpenAi start
```

### 模型配置
打开 appsettings.json，在 "Models" 中添加第三方模型的配置，示例如下：
```json 
{
  "OpenAi": {
    // 允许客户端使用的 apikey 集合，如果为空则客户端可以使用任意 apikey
    "ApiKeys": [
    ],
    "Models": {
      "Glm-4.7-Flash": {
        "Model": "glm-4.7-flash", // Model 缺省时则使用键名作为实际 Model 值    
        "BaseUrl": "https://open.bigmodel.cn/api/paas/v4",
        "ApiKeys": [ "123.xyz", "456.xyz" ],
        "RequestsPerMinute": 1, // 每个 apikey 的每分钟请求数
        "MaxConcurrentRequests": 1 // 每个 apikey 的最大并发数
      },
      "Deepseek-v4-Flash": {
        "Model": "deepseek-v4-flash",
        "BaseUrl": "https://api.deepseek.com",
        "ApiKeys": [ "sk-00000000" ]
      },
      "Deepseek-v4-Flash-Com": {
        "Model": "deepseek-v4-flash",
        "BaseUrl": "https://api.deepseek.com",
        "ApiKeys": [ "sk-11111111" ]
      }
    }
  }
}
```

### 修改 hosts 文件
为了让 Copilot 访问 api.openai.com 时实际请求到 Copilot.Byok.OpenAi，需要在 hosts 文件中添加以下记录：
```
127.0.0.1 api.openai.com
```
 
### 在 Copilot 添加第三方 OpenAI 模型
1. 在 GitHub Copilot 的模型列表中选择"管理模型"，会弹出"自带模型"对话框
2. 在供应商中选择 "OpenAI API"，秘钥输入任意值，点击"添加"即可
3. 模型 ID 需要输入配置中 Models 下级键名，例如 `Deepseek-v4-Flash-Com` 而不是 ~~deepseek-v4-flash~~
4. 如果模型支持 tools 功能，可以勾选"支持工具调用"，这样在 Copilot 的 Agent 模式中也能使用