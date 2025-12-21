<template>
  <div class="prompt-form">
    <div class="form-container">
      <h2 class="form-title">🎨 数学漫画生成器</h2>
      <p class="form-subtitle">输入数学概念，AI将为您生成生动的教育漫画</p>

      <div class="form-group">
        <label for="concept" class="form-label">数学概念</label>
        <input
          id="concept"
          v-model="concept"
          type="text"
          class="form-input"
          placeholder="例如：分数的加法、二次方程、几何图形等"
          :disabled="loading"
        />
        <small class="form-hint">请输入要生成漫画的数学概念</small>
      </div>

      <div class="form-row">
        <div class="form-group">
          <label for="panelCount" class="form-label">面板数量</label>
          <input
            id="panelCount"
            v-model.number="panelCount"
            type="number"
            class="form-input"
            min="3"
            max="6"
            :disabled="loading"
          />
        </div>

        <div class="form-group">
          <label for="ageGroup" class="form-label">年龄组</label>
          <select
            id="ageGroup"
            v-model.number="ageGroup"
            class="form-select"
            :disabled="loading"
          >
            <option :value="0">学龄前 (5-6岁)</option>
            <option :value="1">小学及以上 (6岁以上)</option>
          </select>
        </div>
      </div>

      <button
        @click="generatePrompt"
        class="btn btn-primary"
        :disabled="loading || !concept.trim()"
      >
        <span v-if="loading" class="spinner"></span>
        <span v-else>✨ 生成提示词</span>
      </button>

      <div v-if="error" class="alert alert-error">
        <strong>❌ 错误：</strong>
        <div class="error-message">{{ error }}</div>
        <div v-if="errorDetails && errorDetails.length > 0" class="error-details">
          <strong>解决方案：</strong>
          <ul>
            <li v-for="(detail, index) in errorDetails" :key="index">{{ detail }}</li>
          </ul>
        </div>
      </div>

      <div v-if="result && result.data?.data?.generatedPrompt" class="result-container">
        <div class="result-header">
          <h3>📝 生成的提示词</h3>
          <div class="result-meta">
            <span class="meta-item">⏱️ 处理时间: {{ result.processingTime || result.data?.processingTime }}</span>
            <span v-if="result.requestId" class="meta-item">🆔 请求ID: {{ result.requestId }}</span>
          </div>
        </div>
        
        <div class="prompt-display">
          <pre class="prompt-content">{{ result.data.data.generatedPrompt }}</pre>
        </div>

        <div v-if="result.data.data.suggestions && result.data.data.suggestions.length > 0" class="suggestions">
          <h4>💡 改进建议</h4>
          <ul class="suggestions-list">
            <li v-for="(suggestion, index) in result.data.data.suggestions" :key="index">
              {{ suggestion }}
            </li>
          </ul>
        </div>

        <div class="result-actions">
          <button @click="copyPrompt" class="btn btn-secondary">📋 复制提示词</button>
          <button @click="clearResult" class="btn btn-outline">🗑️ 清除</button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref } from 'vue'
import axios from 'axios'

const concept = ref('加法')
const panelCount = ref(4)
const ageGroup = ref(1)
const loading = ref(false)
const error = ref(null)
const errorDetails = ref([])
const result = ref(null)

// 配置axios以处理HTTPS和自签名证书
axios.defaults.timeout = 120000 // 2分钟超时
if (import.meta.env.DEV) {
  // 开发环境忽略SSL证书错误
  axios.defaults.httpsAgent = false
}

async function generatePrompt() {
  loading.value = true
  error.value = null
  result.value = null

  try {
    const payload = {
      mathConcept: concept.value,
      options: {
        panelCount: panelCount.value,
        ageGroup: ageGroup.value,
        visualStyle: 0, // Cartoon
        language: 0 // Chinese
      }
    }

    console.log('发送请求:', payload)
    
    const resp = await axios.post('/api/comic/generate-prompt', payload, {
      headers: {
        'Content-Type': 'application/json'
      },
      validateStatus: (status) => status < 500 // 允许4xx状态码
    })

    console.log('收到响应:', resp.data)

    if (resp.status === 200 && resp.data) {
      result.value = resp.data
    } else {
      error.value = resp.data?.error || resp.data?.message || `请求失败: ${resp.status}`
      errorDetails.value = resp.data?.details || []
    }
  } catch (ex) {
    console.error('请求错误:', ex)
    error.value = ex?.response?.data?.error || 
                  ex?.response?.data?.message || 
                  ex?.message || 
                  '网络错误，请检查API服务是否运行在 https://localhost:7109'
    
    // 提取错误详情
    errorDetails.value = ex?.response?.data?.details || []
    
    // 如果是网络错误，提供更详细的提示
    if (ex.code === 'ECONNREFUSED' || ex.message?.includes('Network Error')) {
      error.value = '无法连接到API服务器'
      errorDetails.value = [
        '1. 确保API服务正在运行 (https://localhost:7109)',
        '2. 检查appsettings.json中的DeepSeekAPI:ApiKey配置',
        '3. 确认网络连接正常',
        '4. 查看API服务日志获取详细信息'
      ]
    }
  } finally {
    loading.value = false
  }
}

function copyPrompt() {
  if (result.value?.data?.data?.generatedPrompt) {
    navigator.clipboard.writeText(result.value.data.data.generatedPrompt)
      .then(() => {
        alert('提示词已复制到剪贴板！')
      })
      .catch(() => {
        alert('复制失败，请手动复制')
      })
  }
}

function clearResult() {
  result.value = null
  error.value = null
  errorDetails.value = []
}
</script>

<style scoped>
.prompt-form {
  min-height: 100vh;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  padding: 2rem;
  display: flex;
  justify-content: center;
  align-items: flex-start;
}

.form-container {
  background: white;
  border-radius: 16px;
  padding: 2.5rem;
  max-width: 900px;
  width: 100%;
  box-shadow: 0 20px 60px rgba(0, 0, 0, 0.3);
}

.form-title {
  font-size: 2rem;
  font-weight: 700;
  color: #1a202c;
  margin: 0 0 0.5rem 0;
  text-align: center;
}

.form-subtitle {
  color: #718096;
  text-align: center;
  margin-bottom: 2rem;
  font-size: 1rem;
}

.form-group {
  margin-bottom: 1.5rem;
}

.form-label {
  display: block;
  font-weight: 600;
  color: #2d3748;
  margin-bottom: 0.5rem;
  font-size: 0.95rem;
}

.form-input,
.form-select {
  width: 100%;
  padding: 0.75rem 1rem;
  border: 2px solid #e2e8f0;
  border-radius: 8px;
  font-size: 1rem;
  transition: all 0.2s;
  box-sizing: border-box;
}

.form-input:focus,
.form-select:focus {
  outline: none;
  border-color: #667eea;
  box-shadow: 0 0 0 3px rgba(102, 126, 234, 0.1);
}

.form-input:disabled,
.form-select:disabled {
  background-color: #f7fafc;
  cursor: not-allowed;
}

.form-hint {
  display: block;
  color: #718096;
  font-size: 0.875rem;
  margin-top: 0.25rem;
}

.form-row {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 1rem;
}

.btn {
  width: 100%;
  padding: 0.875rem 1.5rem;
  border: none;
  border-radius: 8px;
  font-size: 1rem;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.2s;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
  margin-top: 1rem;
}

.btn-primary {
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: white;
}

.btn-primary:hover:not(:disabled) {
  transform: translateY(-2px);
  box-shadow: 0 10px 20px rgba(102, 126, 234, 0.3);
}

.btn-primary:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.btn-secondary {
  background: #48bb78;
  color: white;
}

.btn-secondary:hover {
  background: #38a169;
}

.btn-outline {
  background: white;
  color: #2d3748;
  border: 2px solid #e2e8f0;
}

.btn-outline:hover {
  border-color: #cbd5e0;
}

.spinner {
  width: 20px;
  height: 20px;
  border: 3px solid rgba(255, 255, 255, 0.3);
  border-top-color: white;
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
}

@keyframes spin {
  to { transform: rotate(360deg); }
}

.alert {
  padding: 1rem;
  border-radius: 8px;
  margin-top: 1rem;
}

.alert-error {
  background-color: #fed7d7;
  color: #c53030;
  border: 1px solid #fc8181;
}

.error-message {
  margin: 0.5rem 0;
  white-space: pre-line;
}

.error-details {
  margin-top: 1rem;
  padding-top: 1rem;
  border-top: 1px solid #fc8181;
}

.error-details ul {
  margin: 0.5rem 0 0 1.5rem;
  padding: 0;
}

.error-details li {
  margin-bottom: 0.25rem;
  line-height: 1.5;
}

.result-container {
  margin-top: 2rem;
  padding-top: 2rem;
  border-top: 2px solid #e2e8f0;
}

.result-header {
  margin-bottom: 1rem;
}

.result-header h3 {
  margin: 0 0 0.5rem 0;
  color: #2d3748;
  font-size: 1.5rem;
}

.result-meta {
  display: flex;
  gap: 1rem;
  flex-wrap: wrap;
  font-size: 0.875rem;
  color: #718096;
}

.meta-item {
  display: flex;
  align-items: center;
  gap: 0.25rem;
}

.prompt-display {
  background: #f7fafc;
  border: 2px solid #e2e8f0;
  border-radius: 8px;
  padding: 1.5rem;
  margin: 1rem 0;
}

.prompt-content {
  margin: 0;
  white-space: pre-wrap;
  word-wrap: break-word;
  font-family: 'Consolas', 'Monaco', 'Courier New', monospace;
  font-size: 0.95rem;
  line-height: 1.6;
  color: #2d3748;
}

.suggestions {
  margin-top: 1.5rem;
  padding: 1rem;
  background: #edf2f7;
  border-radius: 8px;
}

.suggestions h4 {
  margin: 0 0 0.75rem 0;
  color: #2d3748;
  font-size: 1.1rem;
}

.suggestions-list {
  margin: 0;
  padding-left: 1.5rem;
  color: #4a5568;
}

.suggestions-list li {
  margin-bottom: 0.5rem;
  line-height: 1.5;
}

.result-actions {
  display: flex;
  gap: 1rem;
  margin-top: 1.5rem;
}

.result-actions .btn {
  flex: 1;
  margin: 0;
}

@media (max-width: 640px) {
  .form-container {
    padding: 1.5rem;
  }

  .form-row {
    grid-template-columns: 1fr;
  }

  .result-actions {
    flex-direction: column;
  }
}
</style>