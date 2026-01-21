var DiagnosticsPing = {
    init: function() {
        console.log('Initializing Ping Diagnostic...');
        this.bindEvents();
        this.loadRecentTargets();
        setTimeout(() => $('#pingHost').focus(), 100);
    },

    bindEvents: function() {
        $('#pingForm').on('submit', (e) => {
            e.preventDefault();
            this.runPing();
        });

        $('#pingClear').on('click', () => {
            $('#pingOutput').text('No results yet.');
            $('#pingSummary').empty();
        });

        $('#pingCopy').on('click', () => {
            const text = $('#pingOutput').text();
            navigator.clipboard.writeText(text).then(() => {
                this.showStatus('Copied to clipboard', 'success');
            });
        });

        $('#pingHost').on('keydown', (e) => {
            if (e.key === 'Enter' && !e.shiftKey) {
                e.preventDefault();
                $('#pingForm').submit();
            }
        });
    },

    loadRecentTargets: function() {
        const recent = JSON.parse(localStorage.getItem('ping-recent-targets') || '[]');
        if (recent.length > 0) {
            let datalist = document.getElementById('ping-targets-datalist');
            if (!datalist) {
                datalist = document.createElement('datalist');
                datalist.id = 'ping-targets-datalist';
                document.body.appendChild(datalist);
                $('#pingHost').attr('list', 'ping-targets-datalist');
            }
            datalist.innerHTML = recent.map(t => `<option value="${t}">`).join('');
        }
    },

    saveRecentTarget: function(target) {
        const recent = JSON.parse(localStorage.getItem('ping-recent-targets') || '[]');
        if (!recent.includes(target)) {
            recent.unshift(target);
            recent.splice(10);
            localStorage.setItem('ping-recent-targets', JSON.stringify(recent));
            this.loadRecentTargets();
        }
    },

    runPing: function() {
        const payload = {
            target: $('#pingHost').val().trim(),
            count: parseInt($('#pingCount').val(), 10) || 4,
            size: parseInt($('#pingSize').val(), 10) || 56,
            intervalMs: parseInt($('#pingInterval').val(), 10) || 1000,
            timeoutMs: parseInt($('#pingTimeout').val(), 10) || 3000
        };

        if (!payload.target) {
            this.showStatus('Host is required', 'warning');
            return;
        }

        this.saveRecentTarget(payload.target);
        this.showStatus('Running ping...', 'info');

        $.ajax({
            url: '/api/diagnostics/ping',
            method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({
                target: payload.target,
                count: payload.count,
                intervalMs: payload.intervalMs,
                timeoutMs: payload.timeoutMs
            }),
            success: (response) => {
                const extracted = this.extractPlatformData(response);
                if (extracted.success) {
                    this.showStatus('Completed', 'success');
                    this.renderPing(extracted.data);
                } else {
                    this.showStatus(extracted.error || 'Request failed', 'danger');
                }
            },
            error: (xhr) => {
                const message = xhr.responseJSON?.error || xhr.responseText || 'Request failed';
                this.showStatus(message, 'danger');
            }
        });
    },

    extractPlatformData: function(response) {
        const outerSuccess = response?.Success ?? response?.success;
        if (!outerSuccess) {
            return { success: false, error: response?.Error || response?.error };
        }

        const platform = response.Data || response.data || {};
        const platformSuccess = platform.Success ?? platform.success;
        if (!platformSuccess) {
            const err = platform.Error?.Message || platform.error || 'Command failed';
            return { success: false, error: err };
        }

        return { success: true, data: platform.Data || platform.data };
    },

    renderPing: function(data) {
        const summary = [
            { label: 'Transmitted', value: data.Transmitted ?? data.transmitted ?? '-' },
            { label: 'Received', value: data.Received ?? data.received ?? '-' },
            { label: 'Loss', value: (data.LossPercent ?? data.lossPercent ?? 0) + '%' },
            { label: 'Avg', value: (data.AvgMs ?? data.avgMs ?? '-') + ' ms' }
        ];

        $('#pingSummary').html(summary.map(item => `
            <div class="col-md-3">
                <div class="diagnostics-metric">
                    <div class="metric-label">${item.label}</div>
                    <div class="metric-value">${item.value}</div>
                </div>
            </div>
        `).join(''));

        const lines = data.OutputLines || data.outputLines || [];
        const output = data.Output || data.output || data.StdOut || data.stdout || '';
        $('#pingOutput').text(lines.length ? lines.join('\n') : (output || 'No output'));
    },

    showStatus: function(message, type) {
        const alert = $('#diagnosticsStatus');
        alert.removeClass('d-none alert-success alert-danger alert-warning alert-info')
             .addClass(`alert-${type}`)
             .text(message);
        if (type !== 'info') {
            setTimeout(() => alert.addClass('d-none'), 4000);
        }
    }
};

if (typeof Monolith !== 'undefined') {
    Monolith.Pages = Monolith.Pages || {};
    Monolith.Pages.DiagnosticsPing = DiagnosticsPing;
    $(document).ready(() => DiagnosticsPing.init());
}
