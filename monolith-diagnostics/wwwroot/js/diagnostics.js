var Diagnostics = {
    mtrTimer: null,
    mtrLive: false,
    mtrInFlight: false,
    mtrHistory: {},
    mtrLastPayload: null,

    init: function() {
        console.log('Initializing Diagnostics package...');
        this.bindEvents();
    },

    bindEvents: function() {
        $('#pingForm').on('submit', (e) => {
            e.preventDefault();
            this.runPing();
        });

        $('#tracerouteForm').on('submit', (e) => {
            e.preventDefault();
            this.runTraceroute();
        });

        $('#mtrForm').on('submit', (e) => {
            e.preventDefault();
            this.runMtr({ live: false });
        });

        $('#mtrLive').on('click', (e) => {
            e.preventDefault();
            this.startMtrLive();
        });

        $('#mtrStop').on('click', (e) => {
            e.preventDefault();
            this.stopMtrLive(true);
        });

        $('#diagnosticsTabs button[data-bs-toggle="tab"]').on('shown.bs.tab', (e) => {
            if ($(e.target).attr('data-bs-target') !== '#diag-mtr') {
                this.stopMtrLive(false);
            }
        });

        $(window).on('hashchange', () => this.stopMtrLive(false));
        document.addEventListener('visibilitychange', () => {
            if (document.hidden) {
                this.stopMtrLive(false);
            }
        });
    },

    runPing: function() {
        const payload = {
            host: $('#pingHost').val().trim(),
            count: parseInt($('#pingCount').val(), 10) || 4,
            size: parseInt($('#pingSize').val(), 10) || 56,
            intervalMs: parseInt($('#pingInterval').val(), 10) || 1000,
            timeoutMs: parseInt($('#pingTimeout').val(), 10) || 3000
        };

        if (!payload.host) {
            this.showStatus('Host is required', 'warning');
            return;
        }

        this.post('/api/diagnostics/ping', payload, (data) => {
            this.renderPing(data);
        });
    },

    runTraceroute: function() {
        const payload = {
            host: $('#traceHost').val().trim(),
            maxHops: parseInt($('#traceMaxHops').val(), 10) || 30,
            fast: $('#traceFast').is(':checked'),
            resolve: $('#traceResolve').is(':checked')
        };

        if (!payload.host) {
            this.showStatus('Host is required', 'warning');
            return;
        }

        this.post('/api/diagnostics/traceroute', payload, (data) => {
            this.renderTraceroute(data);
        });
    },

    runMtr: function(options) {
        const payload = this.buildMtrPayload();
        if (!payload) {
            return;
        }

        const isLiveTick = options?.live === true;
        if (!isLiveTick && this.mtrLive) {
            this.stopMtrLive(false);
        }
        if (isLiveTick && this.mtrInFlight) {
            return;
        }

        this.mtrInFlight = true;
        this.post('/api/diagnostics/mtr', payload, (data) => {
            this.renderMtr(data);
        }, {
            silent: isLiveTick,
            onComplete: () => {
                this.mtrInFlight = false;
                if (this.mtrLive) {
                    this.scheduleMtr();
                }
            }
        });
    },

    buildMtrPayload: function() {
        const payload = {
            host: $('#mtrHost').val().trim(),
            count: parseInt($('#mtrCount').val(), 10) || 10,
            intervalMs: parseInt($('#mtrInterval').val(), 10) || 1000,
            resolve: $('#mtrResolve').is(':checked')
        };

        if (!payload.host) {
            this.showStatus('Host is required', 'warning');
            return null;
        }

        if (this.mtrLastHost && this.mtrLastHost !== payload.host) {
            this.mtrHistory = {};
        }

        this.mtrLastHost = payload.host;
        return payload;
    },

    startMtrLive: function() {
        const payload = this.buildMtrPayload();
        if (!payload) {
            return;
        }

        this.mtrLive = true;
        this.mtrLastPayload = payload;
        this.mtrHistory = {};
        this.showStatus('Live MTR started', 'info');
        this.scheduleMtr(true);
    },

    scheduleMtr: function(runImmediately) {
        if (!this.mtrLive) {
            return;
        }

        const refreshMs = parseInt($('#mtrRefresh').val(), 10) || 2000;
        if (this.mtrTimer) {
            clearTimeout(this.mtrTimer);
        }

        if (runImmediately) {
            this.runMtr({ live: true });
            return;
        }

        this.mtrTimer = setTimeout(() => {
            this.runMtr({ live: true });
        }, Math.max(1000, refreshMs));
    },

    stopMtrLive: function(showStatus) {
        if (this.mtrTimer) {
            clearTimeout(this.mtrTimer);
            this.mtrTimer = null;
        }

        if (this.mtrLive && showStatus) {
            this.showStatus('Live MTR stopped', 'warning');
        }

        this.mtrLive = false;
        this.mtrInFlight = false;
    },

    post: function(url, payload, onSuccess, options) {
        if (!options?.silent) {
            this.showStatus('Running diagnostics...', 'info');
        }
        $.ajax({
            url: url,
            method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(payload),
            success: (response) => {
                const extracted = this.extractPlatformData(response);
                if (extracted.success) {
                    if (!options?.silent) {
                        this.showStatus('Completed', 'success');
                    }
                    onSuccess(extracted.data);
                } else {
                    this.showStatus(extracted.error || 'Request failed', 'danger');
                }

                if (options?.onComplete) {
                    options.onComplete();
                }
            },
            error: (xhr) => {
                const message = xhr.responseJSON?.error || xhr.responseText || 'Request failed';
                this.showStatus(message, 'danger');
                if (options?.onComplete) {
                    options.onComplete();
                }
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
        $('#pingOutput').text(lines.length ? lines.join('\n') : 'No output');
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
        $('#tracerouteOutput').text(lines.length ? lines.join('\n') : 'No output');
    },

    renderMtr: function(data) {
        const hops = data.Hops || data.hops || [];
        const table = $('#mtrTable');

        if (!hops.length) {
            table.html('<tr><td colspan="9" class="text-muted text-center">No hops returned.</td></tr>');
        } else {
            this.updateMtrHistory(hops);
            table.html(hops.map(hop => `
                <tr>
                    <td>${hop.Hop ?? hop.hop}</td>
                    <td>${this.formatMtrHost(hop.Host ?? hop.host)}</td>
                    <td>${(hop.LossPercent ?? hop.lossPercent ?? 0).toFixed(1)}%</td>
                    <td>${(hop.AvgMs ?? hop.avgMs ?? 0).toFixed(2)}</td>
                    <td>${(hop.LastMs ?? hop.lastMs ?? 0).toFixed(2)}</td>
                    <td>${(hop.BestMs ?? hop.bestMs ?? 0).toFixed(2)}</td>
                    <td>${(hop.WorstMs ?? hop.worstMs ?? 0).toFixed(2)}</td>
                    <td>${(hop.StDevMs ?? hop.stDevMs ?? 0).toFixed(2)}</td>
                    <td>${this.renderSparkline(hop.Hop ?? hop.hop)}</td>
                </tr>
            `).join(''));
        }

        this.renderMtrChart(hops);

        const lines = data.OutputLines || data.outputLines || [];
        $('#mtrOutput').text(lines.length ? lines.join('\n') : 'No output');
    },

    renderMtrChart: function(hops) {
        const container = $('#mtrChart');
        if (!hops.length) {
            container.html('<div class="text-muted">No data.</div>');
            return;
        }

        const avgValues = hops.map(h => h.AvgMs ?? h.avgMs ?? 0);
        const maxAvg = Math.max.apply(null, avgValues.concat([1]));

        const rows = hops.map(hop => {
            const avg = hop.AvgMs ?? hop.avgMs ?? 0;
            const loss = hop.LossPercent ?? hop.lossPercent ?? 0;
            const avgPct = Math.min(100, (avg / maxAvg) * 100);
            const lossPct = Math.min(100, loss);
            const host = hop.Host ?? hop.host;
            const hostParts = this.getHostParts(host);

            return `
                <div class="chart-row">
                    <div class="chart-label">
                        <div class="chart-hop">Hop ${hop.Hop ?? hop.hop}</div>
                        <div class="chart-host">${hostParts.name}</div>
                        <div class="chart-ip">${hostParts.ip}</div>
                    </div>
                    <div class="chart-bars">
                        <div class="chart-bar avg" style="width: ${avgPct}%;"></div>
                        <div class="chart-bar loss" style="width: ${lossPct}%;"></div>
                    </div>
                    <div class="chart-values">${avg.toFixed(1)} ms avg · ${loss.toFixed(1)}% loss</div>
                </div>
            `;
        }).join('');

        container.html(rows);
    },

    updateMtrHistory: function(hops) {
        const maxPoints = 20;
        hops.forEach(hop => {
            const key = hop.Hop ?? hop.hop;
            if (!key) {
                return;
            }
            if (!this.mtrHistory[key]) {
                this.mtrHistory[key] = [];
            }

            const avg = hop.AvgMs ?? hop.avgMs ?? 0;
            this.mtrHistory[key].push(avg);
            if (this.mtrHistory[key].length > maxPoints) {
                this.mtrHistory[key].shift();
            }
        });
    },

    renderSparkline: function(hopKey) {
        const values = this.mtrHistory[hopKey] || [];
        if (!values.length) {
            return '<div class="sparkline empty"></div>';
        }

        const max = Math.max.apply(null, values.concat([1]));
        const bars = values.map(value => {
            const height = Math.max(10, Math.round((value / max) * 100));
            return `<span class="sparkline-bar" style="height: ${height}%"></span>`;
        }).join('');

        return `<div class="sparkline">${bars}</div>`;
    },

    formatMtrHost: function(host) {
        if (!host) {
            return '-';
        }

        const parts = this.getHostParts(host);
        return `<div class="mtr-host">${parts.name}</div><div class="mtr-ip">${parts.ip}</div>`;
    },

    getHostParts: function(host) {
        if (!host) {
            return { name: '-', ip: '' };
        }

        const match = host.match(/^(.+?)\s*\((.+)\)$/);
        if (match) {
            return { name: match[1], ip: match[2] };
        }

        if (this.isIpAddress(host)) {
            return { name: host, ip: 'IP' };
        }

        return { name: host, ip: '' };
    },

    isIpAddress: function(host) {
        return /^[0-9.]+$/.test(host) || host.includes(':');
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
    Monolith.Pages.Diagnostics = Diagnostics;
}
