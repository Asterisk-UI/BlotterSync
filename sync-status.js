// Shows a banner when the server cannot reach Supabase (offline mode) or when
// the browser cannot reach the server. Data entered offline is kept on the
// server and uploaded automatically once the internet is back.
(function () {
    const base = typeof API_BASE_URL !== 'undefined' ? API_BASE_URL : '';
    const banner = document.createElement('div');
    banner.id = 'syncStatusBanner';
    banner.style.cssText = 'position:fixed;left:0;right:0;bottom:0;z-index:9999;padding:8px 16px;' +
        'font:14px/1.4 sans-serif;text-align:center;color:#fff;display:none;';
    document.body.appendChild(banner);

    function show(text, color) {
        banner.textContent = text;
        banner.style.background = color;
        banner.style.display = 'block';
    }

    async function refresh() {
        try {
            const res = await fetch(`${base}/api/Sync/Status`, { cache: 'no-store' });
            if (!res.ok) throw new Error(res.status);
            const s = await res.json();
            const failed = s.failedChanges ? ` ${s.failedChanges} change(s) were rejected by Supabase, ask an admin.` : '';

            if (!s.cloudOnline) {
                const waiting = s.pendingChanges ? ` (${s.pendingChanges} waiting to upload)` : '';
                show(`Offline mode: no connection to Supabase. Work is saved on this server and will upload automatically${waiting}.${failed}`, '#b45309');
            } else if (s.pendingChanges) {
                show(`Uploading ${s.pendingChanges} change(s) to Supabase...${failed}`, '#1d4ed8');
            } else if (s.failedChanges) {
                show(failed.trim(), '#b91c1c');
            } else {
                banner.style.display = 'none';
            }
        } catch {
            show('Cannot reach the BlotterSync server. Check that it is running and connected to this network.', '#b91c1c');
        }
    }

    refresh();
    setInterval(refresh, 10000);
})();
