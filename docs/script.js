fetch('sizes.json')
  .then(r => r.json())
  .then(data => {
    const minMB = (data.min_size / 1e6).toFixed(1);
    const maxMB = (data.max_size / 1e6).toFixed(1);
    
    document.getElementById('self-contained-size').textContent =
      `${minMB} MB`;

    document.getElementById('framework-dependent-size').textContent =
      `${maxMB} MB`;
  });