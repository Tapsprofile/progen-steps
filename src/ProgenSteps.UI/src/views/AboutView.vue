<template>
  <div class="about">
    <h1>About ProgenSteps</h1>
    <p>
      This is a Windows-based application built with modern web technologies.
    </p>
    
    <div class="content">
      <h2>Project Overview</h2>
      <p>
        ProgenSteps demonstrates the integration of ASP.NET Core backend with a Vue 3 frontend,
        providing a foundation for building robust, scalable web applications on Windows.
      </p>

      <h2>Features</h2>
      <ul>
        <li>RESTful API built with ASP.NET Core</li>
        <li>Modern, reactive UI with Vue 3</li>
        <li>TypeScript for type safety</li>
        <li>Client-side routing with Vue Router</li>
        <li>State management with Pinia</li>
        <li>Hot Module Replacement (HMR) for rapid development</li>
      </ul>

      <h2>Development</h2>
      <p>
        The application is designed to run on Windows, leveraging the .NET ecosystem
        for backend services and modern JavaScript tooling for the frontend.
      </p>
    </div>
  </div>
</template>

<style scoped>
.about {
  padding: 2rem;
}

h1 {
  color: #2c3e50;
  margin-bottom: 1rem;
}

.content {
  background-color: #f9f9f9;
  border-radius: 8px;
  padding: 2rem;
  margin-top: 2rem;
}

h2 {
  color: #42b983;
  margin-top: 1.5rem;
  margin-bottom: 0.75rem;
}

h2:first-child {
  margin-top: 0;
}

ul {
  margin-left: 1.5rem;
}

li {
  margin: 0.5rem 0;
  line-height: 1.6;
}

p {
  line-height: 1.6;
  color: #333;
}
</style>
