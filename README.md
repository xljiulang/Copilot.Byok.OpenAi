# Copilot.Byok.OpenAi
让 Visual Studio Copilot 使用第三方兼容 OpenAI API 的模型。


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
  "Models": {
     "glm-4.7-flash": {
      "RequestsPerMinute": 1, // 每个 apikey 的每分钟请求数
      "MaxConcurrentRequests": 1, // 每个 apikey 的最大并发数
      "BaseUrl": "https://open.bigmodel.cn/api/paas/v4",
      "ApiKeys": [
        "123.xyz",
        "456.xyz"
      ]
    },
    "deepseek-v4-flash": {
      "BaseUrl": "https://api.deepseek.com",
      "ApiKeys": [ "sk-123456789" ]
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
3. 模型 ID 需要输入 appsettings.json 中配置的 Model 字段值，例如 `glm-4.7-flash`
4. 如果模型支持 tools 功能，可以勾选"支持工具调用"，这样在 Copilot 的 Agent 模式中也能使用