var DiagnosticsMtr = {
    mtrTimer: null,
    mtrLive: false,
    mtrInFlight: false,
    mtrHistory: {},
    mtrLastPayload: null,
    mtrLastHost: null,
    charts: {},
    hopData: [],
    latencyData: [],
    packetLossData: [],

    init: function() {
        console.log('Initializing MTR Diagnostic...');
        this.bindEvents();
        this.initializeCharts();
        this.loadRecentTargets();
        setTimeout(() => $('#mtrHost').focus(), 100);
    },

    bindEvents: function() {
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

        $('#mtrCopy').on('click', () => {
            const text = $('#mtrOutput').text();
            navigator.clipboard.writeText(text).then(() => {
                this.showStatus('Copied to clipboard', 'success');
            });
        });

        $('#mtrHost').on('keydown', (e) => {
            if (e.key === 'Enter' && !e.shiftKey) {
                e.preventDefault();
                $('#mtrForm').submit();
            }
        });

        $('#mtr-graphs-tab').on('shown.bs.tab', () => {
            this.updateGraphs();
        });

        $('#mtr-stats-tab').on('shown.bs.tab', () => {
            this.updateStats();
        });

        $(window).on('hashchange', () => this.stopMtrLive(false));
        document.addEventListener('visibilitychange', () => {
            if (document.hidden) {
                this.stopMtrLive(false);
            }
        });
    },

    loadRecentTargets: function() {
        const recent = JSON.parse(localStorage.getItem('mtr-recent-targets') || '[]');
        if (recent.length > 0) {
            let datalist = document.getElementById('mtr-targets-datalist');
            if (!datalist) {
                datalist = document.createElement('datalist');
                datalist.id = 'mtr-targets-datalist';
                document.body.appendChild(datalist);
                $('#mtrHost').attr('list', 'mtr-targets-datalist');
            }
            datalist.innerHTML = recent.map(t => `<option value="${t}">`).join('');
        }
    },

    saveRecentTarget: function(target) {
        const recent = JSON.parse(localStorage.getItem('mtr-recent-targets') || '[]');
        if (!recent.includes(target)) {
            recent.unshift(target);
            recent.splice(10);
            localStorage.setItem('mtr-recent-targets', JSON.stringify(recent));
            this.loadRecentTargets();
        }
    },

    initializeCharts: function() {
        if (typeof Chart === 'undefined') {
            console.warn('Chart.js not loaded');
            return;
        }

        $('#mtrGraphsContainer').html(`
            <div class="mb-4">
                <canvas id="mtr-latency-chart"></canvas>
            </div>
            <div>
                <canvas id="mtr-packetloss-chart"></canvas>
            </div>
        `);

        const latencyCtx = document.getElementById('mtr-latency-chart');
        if (latencyCtx) {
            this.charts.latency = new Chart(latencyCtx, {
                type: 'line',
                data: {
                    labels: [],
                    datasets: [{
                        label: 'Min Latency (ms)',
                        data: [],
                        borderColor: 'rgb(59, 130, 246)',
                        backgroundColor: 'rgba(59, 130, 246, 0.1)',
                        tension: 0.4
                    }, {
                        label: 'Avg Latency (ms)',
                        data: [],
                        borderColor: 'rgb(34, 197, 94)',
                        backgroundColor: 'rgba(34, 197, 94, 0.1)',
                        tension: 0.4
                    }, {
                        label: 'Max Latency (ms)',
                        data: [],
                        borderColor: 'rgb(239, 68, 68)',
                        backgroundColor: 'rgba(239, 68, 68, 0.1)',
                        tension: 0.4
                    }]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: true,
                    aspectRatio: 2,
                    plugins: {
                        title: { display: true, text: 'Latency Over Time' },
                        legend: { display: true }
                    },
                    scales: {
                        y: { beginAtZero: true, title: { display: true, text: 'Latency (ms)' } },
                        x: { title: { display: true, text: 'Time' } }
                    }
                }
            });
        }

        const packetLossCtx = document.getElementById('mtr-packetloss-chart');
        if (packetLossCtx) {
            this.charts.packetLoss = new Chart(packetLossCtx, {
                type: 'bar',
                data: {
                    labels: [],
                    datasets: [{
                        label: 'Packet Loss (%)',
                        data: [],
                        backgroundColor: 'rgba(239, 68, 68, 0.6)',
                        borderColor: 'rgb(239, 68, 68)',
                        borderWidth: 1
                    }]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: true,
                    aspectRatio: 2,
                    plugins: {
                        title: { display: true, text: 'Packet Loss by Hop' },
                        legend: { display: false }
                    },
                    scales: {
                        y: { beginAtZero: true, max: 100, title: { display: true, text: 'Packet Loss (%)' } },
                        x: { title: { display: true, text: 'Hop Number' } }
                    }
                }
            });
        }
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
        if (!isLiveTick) {
            this.hopData = [];
            this.latencyData = [];
            this.packetLossData = [];
        }

        this.post('/api/diagnostics/mtr', payload, (data) => {
            this.renderMtr(data);
            this.parseMtrData(data);
            this.updateGraphs();
            this.updateStats();
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
            target: $('#mtrHost').val().trim(),
            count: parseInt($('#mtrCount').val(), 10) || 10,
            intervalMs: parseInt($('#mtrInterval').val(), 10) || 1000
        };

        if (!payload.target) {
            this.showStatus('Host is required', 'warning');
            return null;
        }

        this.saveRecentTarget(payload.target);

        if (this.mtrLastHost && this.mtrLastHost !== payload.target) {
            this.mtrHistory = {};
            this.hopData = [];
            this.latencyData = [];
            this.packetLossData = [];
        }

        this.mtrLastHost = payload.target;
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
        $('#mtrLive').hide();
        $('#mtrStop').show();
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
        $('#mtrLive').show();
        $('#mtrStop').hide();
    },

    post: function(url, payload, onSuccess, options) {
        if (!options?.silent) {
            this.showStatus('Running MTR...', 'info');
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

    renderMtr: function(data) {
        const hops = data.Hops || data.hops || [];
        const table = $('#mtrTable');

        if (!hops.length) {
            table.html('<tr><td colspan="8" class="text-muted text-center">No hops returned.</td></tr>');
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
                </tr>
            `).join(''));
        }

        const lines = data.OutputLines || data.outputLines || [];
        const output = data.Output || data.output || data.StdOut || data.stdout || '';
        $('#mtrOutput').text(lines.length ? lines.join('\n') : (output || 'No output'));
    },

    parseMtrData: function(data) {
        const hops = data.Hops || data.hops || [];
        if (!hops.length) return;

        const currentTime = this.latencyData.length;
        hops.forEach(hop => {
            const hopNum = hop.Hop ?? hop.hop;
            const avg = hop.AvgMs ?? hop.avgMs ?? 0;
            const best = hop.BestMs ?? hop.bestMs ?? 0;
            const worst = hop.WorstMs ?? hop.worstMs ?? 0;
            const loss = hop.LossPercent ?? hop.lossPercent ?? 0;

            this.latencyData.push({
                time: currentTime,
                min: best,
                avg: avg,
                max: worst
            });

            const existingHop = this.hopData.find(h => h.hop === hopNum);
            if (existingHop) {
                existingHop.latencies.min.push(best);
                existingHop.latencies.avg.push(avg);
                existingHop.latencies.max.push(worst);
                existingHop.losses.push(loss);
            } else {
                this.hopData.push({
                    hop: hopNum,
                    host: hop.Host ?? hop.host,
                    latencies: { min: [best], avg: [avg], max: [worst] },
                    losses: [loss]
                });
            }
        });

        this.packetLossData = this.hopData.map(h => ({
            hop: h.hop,
            host: h.host,
            loss: h.losses.reduce((a, b) => a + b, 0) / h.losses.length
        }));
    },

    updateMtrHistory: function(hops) {
        const maxPoints = 20;
        hops.forEach(hop => {
            const key = hop.Hop ?? hop.hop;
            if (!key) return;
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

    updateGraphs: function() {
        if (!this.charts.latency || !this.charts.packetLoss) return;

        if (this.latencyData.length > 0) {
            const labels = this.latencyData.map((_, i) => `T${i + 1}`);
            this.charts.latency.data.labels = labels;
            this.charts.latency.data.datasets[0].data = this.latencyData.map(d => d.min);
            this.charts.latency.data.datasets[1].data = this.latencyData.map(d => d.avg);
            this.charts.latency.data.datasets[2].data = this.latencyData.map(d => d.max);
            this.charts.latency.update();
        }

        if (this.packetLossData.length > 0) {
            const labels = this.packetLossData.map(d => `Hop ${d.hop}`);
            this.charts.packetLoss.data.labels = labels;
            this.charts.packetLoss.data.datasets[0].data = this.packetLossData.map(d => d.loss);
            this.charts.packetLoss.update();
        }
    },

    updateStats: function() {
        if (this.hopData.length === 0) {
            $('#mtrStatsContainer').html('<div class="text-center text-muted py-5"><p>No statistics available</p></div>');
            return;
        }

        let html = '<div class="table-responsive"><table class="table table-sm table-hover">';
        html += '<thead><tr><th>Hop</th><th>Host</th><th>Avg Loss %</th><th>Min Latency</th><th>Avg Latency</th><th>Max Latency</th></tr></thead><tbody>';

        this.hopData.forEach(hop => {
            const avgLoss = hop.losses.reduce((a, b) => a + b, 0) / hop.losses.length;
            const minLat = Math.min(...hop.latencies.min);
            const avgLat = hop.latencies.avg.reduce((a, b) => a + b, 0) / hop.latencies.avg.length;
            const maxLat = Math.max(...hop.latencies.max);

            html += `<tr>
                <td>${hop.hop}</td>
                <td><code>${hop.host}</code></td>
                <td>${avgLoss.toFixed(2)}%</td>
                <td>${minLat.toFixed(2)}ms</td>
                <td>${avgLat.toFixed(2)}ms</td>
                <td>${maxLat.toFixed(2)}ms</td>
            </tr>`;
        });

        html += '</tbody></table></div>';
        $('#mtrStatsContainer').html(html);
    },

    formatMtrHost: function(host) {
        if (!host) return '-';
        const parts = this.getHostParts(host);
        return `<div class="mtr-host">${parts.name}</div><div class="mtr-ip">${parts.ip}</div>`;
    },

    getHostParts: function(host) {
        if (!host) return { name: '-', ip: '' };
        const match = host.match(/^(.+?)\s*\((.+)\)$/);
        if (match) return { name: match[1], ip: match[2] };
        if (this.isIpAddress(host)) return { name: host, ip: 'IP' };
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
    Monolith.Pages.DiagnosticsMtr = DiagnosticsMtr;
    $(document).ready(() => DiagnosticsMtr.init());
}
