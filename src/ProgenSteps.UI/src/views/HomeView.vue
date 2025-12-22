<script setup lang="ts">
import { ref, onMounted } from 'vue'

const message = ref<string>('Loading...')
const weatherData = ref<any[]>([])

const fetchWeatherForecast = async () => {
  try {
    const response = await fetch('/api/weatherforecast')
    if (response.ok) {
      weatherData.value = await response.json()
      message.value = 'Connected to ASP.NET API'
    } else {
      message.value = 'API available but returned error'
    }
  } catch (error) {
    message.value = 'API not available (this is expected in development)'
  }
}

onMounted(() => {
  fetchWeatherForecast()
})
</script>

<template>
  <div class="home">
    <h1>Welcome to ProgenSteps</h1>
    <p>A Windows-based application using ASP.NET and Vue3</p>
    
    <div class="info-box">
      <h2>Technology Stack</h2>
      <ul>
        <li><strong>Backend:</strong> ASP.NET Core Web API (C#)</li>
        <li><strong>Frontend:</strong> Vue 3 with TypeScript</li>
        <li><strong>State Management:</strong> Pinia</li>
        <li><strong>Routing:</strong> Vue Router</li>
        <li><strong>Build Tool:</strong> Vite</li>
      </ul>
    </div>

    <div class="api-status">
      <h2>API Status</h2>
      <p>{{ message }}</p>
      <div v-if="weatherData.length > 0" class="weather-data">
        <h3>Weather Forecast from API:</h3>
        <table>
          <thead>
            <tr>
              <th>Date</th>
              <th>Temperature (°C)</th>
              <th>Summary</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="(item, index) in weatherData" :key="index">
              <td>{{ item.date }}</td>
              <td>{{ item.temperatureC }}</td>
              <td>{{ item.summary }}</td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  </div>
</template>

<style scoped>
.home {
  padding: 2rem;
}

h1 {
  color: #2c3e50;
  margin-bottom: 1rem;
}

.info-box {
  background-color: #f5f5f5;
  border-radius: 8px;
  padding: 1.5rem;
  margin: 2rem 0;
}

.info-box h2 {
  color: #42b983;
  margin-bottom: 1rem;
}

.info-box ul {
  list-style: none;
  padding: 0;
}

.info-box li {
  padding: 0.5rem 0;
  border-bottom: 1px solid #ddd;
}

.info-box li:last-child {
  border-bottom: none;
}

.api-status {
  background-color: #e8f5e9;
  border-radius: 8px;
  padding: 1.5rem;
  margin: 2rem 0;
}

.api-status h2 {
  color: #2c3e50;
  margin-bottom: 1rem;
}

.weather-data {
  margin-top: 1rem;
}

table {
  width: 100%;
  border-collapse: collapse;
  margin-top: 1rem;
}

th, td {
  padding: 0.75rem;
  text-align: left;
  border-bottom: 1px solid #ddd;
}

th {
  background-color: #42b983;
  color: white;
  font-weight: bold;
}

tr:hover {
  background-color: #f5f5f5;
}
</style>
