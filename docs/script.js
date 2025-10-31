const fdBtn = document.getElementById('fd-download');
const scBtn = document.getElementById('sc-download');
const fdInfo = document.getElementById('fd-info');
const scInfo = document.getElementById('sc-info');

// fallback links in case fetch or JSON parsing fails
const fallback = {
  sc: 'https://github.com/FrostSource/AlyxLibInstaller/releases/latest/download/AlyxLibInstaller-SC.exe',
  fd: 'https://github.com/FrostSource/AlyxLibInstaller/releases/latest/download/AlyxLibInstaller-FD.exe',
};

// a tiny helper to format file size nicely
const formatMB = b => (b / 1e6).toFixed(1) + ' MB';

fetch('sizes.json')
  .then(r => {
    if (!r.ok) throw new Error('network');
    return r.json();
  })
  .then(data => {
    // Find the matching files
    const sc = data.files.find(f => /(sc)|(selfcontained)/i.test(f.name));
    const fd = data.files.find(f => /(fd)|(frameworkdependent)/i.test(f.name));

    if (sc) {
      scBtn.href = sc.url;
      scInfo.textContent = `${data.version} • ${formatMB(sc.size)}`;
    }
    else {
      scBtn.href = fallback.sc;
      scInfo.textContent = `${data.version} • ${formatMB(data.max_size)}`;
    }

    // Update button links
    if (fd) {
      fdBtn.href = fd.url;
      fdInfo.textContent = `${data.version} • ${formatMB(fd.size)}`;
    }
    else {
      fdBtn.href = fallback.fd;
      fdInfo.textContent = `${data.version} • ${formatMB(data.max_size)}`;
    }
  })
  .catch(err => {
    console.error('Failed to load sizes.json', err);

    // fallback links
    scBtn.href = fallback.sc;
    fdBtn.href = fallback.fd;

    scInfo.textContent = "latest version";
    fdInfo.textContent = "latest version";
  });
