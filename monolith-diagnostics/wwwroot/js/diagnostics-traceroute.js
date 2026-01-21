var DiagnosticsTraceroute = {
    init: function() {
        console.log('Initializing Traceroute Diagnostic...');
        this.bindEvents();
        setTimeout(() => $('#traceHost').focus(), 100);
    },

    bindEvents: function() {
        $('#tracerouteForm').on('submit', (e) => {
            e.preventDefault();
            this.runTraceroute();
        });

        $('#traceClear').on('click', () => {
            $('#tracerouteOutput').text('No results yet.');
            $('#tracerouteTable').html('<tr><td colspan="3" class="text-muted text-center">No results yet.</td></tr>');
        });

        $('#traceCopy').on('click', () => {
            const text = $('#tracerouteOutput').text();
            navigator.clipboard.writeText(text).then(() => {
                this.showStatus('Copied to clipboard', 'success');
            });
        });

        $('#traceHost').on('keydown', (e) => {
            if (e.key === 'Enter' && !e.shiftKey) {
                e.preventDefault();
                $('#tracerouteForm').submit();
            }
        });

        $('#traceFast').on('change', function() {
            if ($(this).is(':checked')) {
                $('#traceMaxHops').val(20);
            } else {
                $('#traceMaxHops').val(30);
            }
        });
    },

    runTraceroute: function() {
        const payload = {
            target: $('#traceHost').val().trim(),
            maxHops: parseInt($('#traceMaxHops').val(), 10) || 30,
            fast: $('#traceFast').is(':checked'),
            waitMs: $('#traceFast').is(':checked') ? 1000 : 3000
        };

        if (!payload.target) {
            this.showStatus('Host is required', 'warning');
            return;
        }

        this.showStatus('Running traceroute...', 'info');

        $.ajax({
            url: '/api/diagnostics/traceroute',
            method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(payload),
            success: (response) => {
                const extracted = this.extractPlatformData(response);
                if (extracted.success) {
                    this.showStatus('Completed', 'success');
                    this.renderTraceroute(extracted.data);
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

    renderTraceroute: function(data) {
        const hops = data.Hops || data.hops || [];
        const table = $('#tracerouteTable');

        if (!hops.length) {
            table.html('<tr><td colspan="3" class="text-muted text-center">No hops returned.</td></tr>');
        } else {
            table.html(hops.map(hop => `
                <tr>
                    <td>${hop.Hop ?? hop.hop}</td>
                    <td>${hop.Host ?? hop.host}</td>
                    <td>${(hop.TimesMs || hop.timesMs || []).map(t => t.toFixed(2)).join(', ') || '-'}</td>
                </tr>
            `).join(''));
        }

        const lines = data.OutputLines || data.outputLines || [];
        const output = data.Output || data.output || data.StdOut || data.stdout || '';
        $('#tracerouteOutput').text(lines.length ? lines.join('\n') : (output || 'No output'));
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
    Monolith.Pages.DiagnosticsTraceroute = DiagnosticsTraceroute;
    $(document).ready(() => DiagnosticsTraceroute.init());
}
