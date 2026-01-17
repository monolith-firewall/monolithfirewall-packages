// DNS Configuration JavaScript with Tabs

$(document).ready(function() {
    // Load settings
    loadSettings();
    
    // Load zones
    loadZones();
    
    // Load records
    loadRecords();

    // Handle settings form submission
    $('#settingsForm').on('submit', function(e) {
        e.preventDefault();
        saveSettings();
    });

    // Handle reset settings button
    $('#resetSettingsBtn').on('click', function() {
        loadSettings();
    });

    // Handle refresh records button
    $('#refreshRecordsBtn').on('click', function() {
        loadRecords();
    });

    // Handle forwarding toggle
    $('#forwarding').on('change', function() {
        if ($(this).is(':checked')) {
            $('#forwardersRow').show();
        } else {
            $('#forwardersRow').hide();
        }
    });

    // Handle zone filter change
    $('#zoneFilter').on('change', function() {
        loadRecords();
    });
});

function loadSettings() {
    $.ajax({
        url: '/api/packages/monolith-network/modules/dns/get-settings?_=' + new Date().getTime(),
        method: 'GET',
        cache: false,
        headers: {
            'X-Requested-With': 'XMLHttpRequest'
        },
        success: function(response) {
            // Handle both uppercase and lowercase property names
            if ((response.success || response.Success) && (response.data || response.Data)) {
                var settings = response.data || response.Data;
                $('#dnsEnabled').prop('checked', settings.enabled || settings.Enabled || false);
                $('#recursion').prop('checked', settings.recursion !== undefined ? (settings.recursion || settings.Recursion) : true);
                $('#forwarding').prop('checked', settings.forwarding || settings.Forwarding || false);
                $('#dnssecValidation').prop('checked', settings.dnssecValidation !== undefined ? (settings.dnssecValidation || settings.DnssecValidation) : true);
                $('#logLevel').val(settings.logLevel || settings.LogLevel || 'info');
                
                // Handle forwarders
                var forwarders = settings.forwarders || settings.Forwarders || [];
                if (Array.isArray(forwarders) && forwarders.length > 0) {
                    $('#forwarders').val(forwarders.join(', '));
                }
                
                // Show/hide forwarders row
                if ($('#forwarding').is(':checked')) {
                    $('#forwardersRow').show();
                } else {
                    $('#forwardersRow').hide();
                }
            }
        },
        error: function(xhr, status, error) {
            showMessage('Failed to load settings: ' + error, 'danger');
        }
    });
}

function saveSettings() {
    var forwardersText = $('#forwarders').val().trim();
    var forwarders = forwardersText ? forwardersText.split(',').map(f => f.trim()).filter(f => f) : [];

    var settings = {
        enabled: $('#dnsEnabled').is(':checked'),
        recursion: $('#recursion').is(':checked'),
        forwarding: $('#forwarding').is(':checked'),
        forwarders: forwarders,
        dnssecValidation: $('#dnssecValidation').is(':checked'),
        logLevel: $('#logLevel').val()
    };

    $.ajax({
        url: '/api/packages/monolith-network/modules/dns/update-settings',
        method: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(settings),
        headers: {
            'X-Requested-With': 'XMLHttpRequest'
        },
        success: function(response) {
            // Handle both uppercase and lowercase property names
            if (response.success || response.Success) {
                showMessage('Settings saved successfully!', 'success');
            } else {
                showMessage('Failed to save settings: ' + (response.error || response.Error || 'Unknown error'), 'danger');
            }
        },
        error: function(xhr, status, error) {
            showMessage('Failed to save settings: ' + error, 'danger');
        }
    });
}

function loadZones() {
    $.ajax({
        url: '/api/packages/monolith-network/modules/dns/get-zones?_=' + new Date().getTime(),
        method: 'GET',
        cache: false,
        headers: {
            'X-Requested-With': 'XMLHttpRequest'
        },
        success: function(response) {
            console.log('Zones response:', response);
            // Handle both uppercase and lowercase property names
            if ((response.success || response.Success) && (response.data || response.Data)) {
                renderZones(response.data || response.Data);
            } else {
                console.error('Invalid response format:', response);
                showMessage('Failed to load zones: Invalid response format', 'danger');
            }
        },
        error: function(xhr, status, error) {
            console.error('AJAX error:', xhr, status, error);
            console.error('Response text:', xhr.responseText);
            showMessage('Failed to load zones: ' + (xhr.responseText || error), 'danger');
        }
    });
}

// Helper function to normalize zone object (handle both PascalCase and camelCase)
function normalizeZone(zone) {
    return {
        name: zone.name || zone.Name || '',
        type: zone.type || zone.Type || 'master',
        enabled: zone.enabled !== undefined ? zone.enabled : (zone.Enabled !== undefined ? zone.Enabled : false),
        file: zone.file || zone.File || '',
        masters: zone.masters || zone.Masters || [],
        allowTransfer: zone.allowTransfer !== undefined ? zone.allowTransfer : (zone.AllowTransfer !== undefined ? zone.AllowTransfer : false),
        allowTransferTo: zone.allowTransferTo || zone.AllowTransferTo || [],
        ttl: zone.ttl || zone.Ttl || 3600,
        soaEmail: zone.soaEmail || zone.SoaEmail || 'admin@example.com',
        refresh: zone.refresh || zone.Refresh || 86400,
        retry: zone.retry || zone.Retry || 7200,
        expire: zone.expire || zone.Expire || 604800,
        negativeTtl: zone.negativeTtl || zone.NegativeTtl || 3600
    };
}

function renderZones(zones) {
    var tabsHtml = '';
    var contentHtml = '';
    
    if (zones && zones.length > 0) {
        zones.forEach(function(zoneRaw, index) {
            // Normalize zone object to handle PascalCase/camelCase
            var zone = normalizeZone(zoneRaw);
            
            var isActive = index === 0 ? 'active' : '';
            var isSelected = index === 0 ? 'true' : 'false';
            var isShow = index === 0 ? 'show active' : '';
            
            // Tab button
            tabsHtml += `
                <li class="nav-item" role="presentation">
                    <button class="nav-link ${isActive}" id="zone-${zone.name.replace(/\./g, '-')}-tab" 
                            data-bs-toggle="pill" data-bs-target="#zone-${zone.name.replace(/\./g, '-')}" 
                            type="button" role="tab" aria-controls="zone-${zone.name.replace(/\./g, '-')}" 
                            aria-selected="${isSelected}">
                        <span class="badge bg-${zone.enabled ? 'success' : 'secondary'} me-1"></span>
                        ${zone.name}
                    </button>
                </li>
            `;
            
            // Tab content
            contentHtml += `
                <div class="tab-pane fade ${isShow}" id="zone-${zone.name.replace(/\./g, '-')}" role="tabpanel" 
                     aria-labelledby="zone-${zone.name.replace(/\./g, '-')}-tab">
                    ${renderZoneConfig(zone)}
                </div>
            `;
        });
    } else {
        tabsHtml = '<li class="nav-item"><span class="text-muted">No zones available</span></li>';
    }
    
    $('#zoneTabs').html(tabsHtml);
    $('#zoneTabContent').html(contentHtml);
    
    // Update zone filter dropdown
    updateZoneFilter(zones);
    
    // Attach event handlers
    $('.zone-config-form').on('submit', function(e) {
        e.preventDefault();
        saveZoneConfig($(this));
    });
}

function updateZoneFilter(zones) {
    var $filter = $('#zoneFilter');
    $filter.find('option:not(:first)').remove();
    
    if (zones && zones.length > 0) {
        zones.forEach(function(zoneRaw) {
            var zone = normalizeZone(zoneRaw);
            $filter.append(`<option value="${zone.name}">${zone.name}</option>`);
        });
    }
}

function renderZoneConfig(zone) {
    var zoneId = zone.name.replace(/\./g, '-');
    return `
        <form class="zone-config-form" data-zone="${zone.name}">
            <div class="row mb-3">
                <label class="col-sm-3 col-form-label fw-bold">Enable</label>
                <div class="col-sm-9">
                    <div class="form-check form-switch">
                        <input class="form-check-input" type="checkbox" 
                               id="enabled-${zoneId}" 
                               ${zone.enabled ? 'checked' : ''}>
                        <label class="form-check-label" for="enabled-${zoneId}">
                            Enable DNS zone: ${zone.name}
                        </label>
                    </div>
                </div>
            </div>

            <div class="row mb-3">
                <label class="col-sm-3 col-form-label">Zone Type</label>
                <div class="col-sm-9">
                    <select class="form-select" id="type-${zoneId}">
                        <option value="master" ${zone.type === 'master' ? 'selected' : ''}>Master</option>
                        <option value="slave" ${zone.type === 'slave' ? 'selected' : ''}>Slave</option>
                        <option value="forward" ${zone.type === 'forward' ? 'selected' : ''}>Forward</option>
                        <option value="stub" ${zone.type === 'stub' ? 'selected' : ''}>Stub</option>
                    </select>
                    <div class="form-text">
                        Master: Authoritative zone. Slave: Replicated from master. Forward: Forward queries. Stub: Forward zone transfers.
                    </div>
                </div>
            </div>

            <div class="row mb-3">
                <label class="col-sm-3 col-form-label">Zone File</label>
                <div class="col-sm-9">
                    <input type="text" class="form-control" id="file-${zoneId}" 
                           value="${zone.file || ''}" placeholder="/etc/bind/db.example.com">
                    <div class="form-text">Path to zone file (for master zones).</div>
                </div>
            </div>

            <div class="row mb-3" id="mastersRow-${zoneId}" style="display: ${zone.type === 'slave' ? 'block' : 'none'};">
                <label class="col-sm-3 col-form-label">Master Servers</label>
                <div class="col-sm-9">
                    <input type="text" class="form-control" id="masters-${zoneId}" 
                           value="${(zone.masters || []).join(', ')}" placeholder="192.168.1.1, 192.168.1.2">
                    <div class="form-text">Comma-separated list of master DNS server IP addresses (for slave zones).</div>
                </div>
            </div>

            <div class="row mb-3">
                <label class="col-sm-3 col-form-label">Default TTL</label>
                <div class="col-sm-9">
                    <input type="number" class="form-control" id="ttl-${zoneId}" 
                           value="${zone.ttl || 3600}" min="60">
                    <div class="form-text">Default TTL for records in this zone (seconds).</div>
                </div>
            </div>

            <div class="row mb-3">
                <label class="col-sm-3 col-form-label">SOA Email</label>
                <div class="col-sm-9">
                    <input type="text" class="form-control" id="soaEmail-${zoneId}" 
                           value="${zone.soaEmail || ''}" placeholder="admin@example.com">
                    <div class="form-text">Email address for the Start of Authority (SOA) record (use @ instead of .).</div>
                </div>
            </div>

            <div class="row mb-3">
                <label class="col-sm-3 col-form-label">SOA Refresh</label>
                <div class="col-sm-9">
                    <input type="number" class="form-control" id="refresh-${zoneId}" 
                           value="${zone.refresh || 86400}" min="60">
                    <div class="form-text">SOA refresh interval in seconds (default: 86400 = 24 hours).</div>
                </div>
            </div>

            <div class="row mb-3">
                <label class="col-sm-3 col-form-label">SOA Retry</label>
                <div class="col-sm-9">
                    <input type="number" class="form-control" id="retry-${zoneId}" 
                           value="${zone.retry || 7200}" min="60">
                    <div class="form-text">SOA retry interval in seconds (default: 7200 = 2 hours).</div>
                </div>
            </div>

            <div class="row mb-3">
                <label class="col-sm-3 col-form-label">SOA Expire</label>
                <div class="col-sm-9">
                    <input type="number" class="form-control" id="expire-${zoneId}" 
                           value="${zone.expire || 604800}" min="60">
                    <div class="form-text">SOA expire interval in seconds (default: 604800 = 7 days).</div>
                </div>
            </div>

            <div class="row mb-3">
                <label class="col-sm-3 col-form-label">SOA Negative TTL</label>
                <div class="col-sm-9">
                    <input type="number" class="form-control" id="negativeTtl-${zoneId}" 
                           value="${zone.negativeTtl || 3600}" min="60">
                    <div class="form-text">SOA negative TTL (NXDOMAIN cache time) in seconds (default: 3600 = 1 hour).</div>
                </div>
            </div>

            <div class="row mb-3">
                <label class="col-sm-3 col-form-label">Allow Zone Transfer</label>
                <div class="col-sm-9">
                    <div class="form-check form-switch">
                        <input class="form-check-input" type="checkbox" 
                               id="allowTransfer-${zoneId}" 
                               ${zone.allowTransfer ? 'checked' : ''}>
                        <label class="form-check-label" for="allowTransfer-${zoneId}">
                            Allow zone transfers
                        </label>
                    </div>
                    <div class="form-text">
                        Allow slave servers to transfer this zone.
                    </div>
                </div>
            </div>

            <hr class="my-4">

            <div class="d-flex gap-2">
                <button type="submit" class="btn btn-primary">
                    <svg width="16" height="16" fill="currentColor" viewBox="0 0 16 16" class="me-1">
                        <path d="M15.854.146a.5.5 0 0 1 .11.54l-5.819 14.547a.75.75 0 0 1-1.329.124l-3.178-4.995L.643 7.184a.75.75 0 0 1 .124-1.33L15.314.037a.5.5 0 0 1 .54.11ZM6.636 10.07l2.761 4.338L14.13 2.576 6.636 10.07Zm6.787-8.201L1.591 6.602l4.339 2.76 7.494-7.493Z"/>
                    </svg>
                    Save Configuration
                </button>
                <button type="button" class="btn btn-outline-secondary" onclick="loadZones()">Reset</button>
            </div>
        </form>
    `;
}

function saveZoneConfig($form) {
    var zoneName = $form.data('zone');
    var zoneId = zoneName.replace(/\./g, '-');
    
    var mastersText = $(`#masters-${zoneId}`).val().trim();
    var masters = mastersText ? mastersText.split(',').map(m => m.trim()).filter(m => m) : [];
    
    var config = {
        zone: zoneName,
        enabled: $(`#enabled-${zoneId}`).is(':checked'),
        type: $(`#type-${zoneId}`).val(),
        file: $(`#file-${zoneId}`).val(),
        masters: masters,
        allowTransfer: $(`#allowTransfer-${zoneId}`).is(':checked'),
        allowTransferTo: [],
        ttl: parseInt($(`#ttl-${zoneId}`).val()),
        soaEmail: $(`#soaEmail-${zoneId}`).val(),
        refresh: parseInt($(`#refresh-${zoneId}`).val()),
        retry: parseInt($(`#retry-${zoneId}`).val()),
        expire: parseInt($(`#expire-${zoneId}`).val()),
        negativeTtl: parseInt($(`#negativeTtl-${zoneId}`).val())
    };

    $.ajax({
        url: '/api/packages/monolith-network/modules/dns/update-zone',
        method: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(config),
        headers: {
            'X-Requested-With': 'XMLHttpRequest'
        },
        success: function(response) {
            // Handle both uppercase and lowercase property names
            if (response.success || response.Success) {
                showMessage(`Configuration saved for ${zoneName}!`, 'success');
                // Reload zones to update status badges
                loadZones();
            } else {
                showMessage('Failed to save configuration: ' + (response.error || response.Error || 'Unknown error'), 'danger');
            }
        },
        error: function(xhr, status, error) {
            showMessage('Failed to save configuration: ' + error, 'danger');
        }
    });
}

function loadRecords() {
    var zoneFilter = $('#zoneFilter').val();
    var url = '/api/packages/monolith-network/modules/dns/get-records?_=' + new Date().getTime();
    if (zoneFilter) {
        url += '&zone=' + encodeURIComponent(zoneFilter);
    }
    
    $.ajax({
        url: url,
        method: 'GET',
        cache: false,
        headers: {
            'X-Requested-With': 'XMLHttpRequest'
        },
        success: function(response) {
            // Handle both uppercase and lowercase property names
            if ((response.success || response.Success) && (response.data || response.Data)) {
                renderRecords(response.data || response.Data);
            } else {
                // Empty records - still render empty table
                renderRecords([]);
            }
        },
        error: function(xhr, status, error) {
            showMessage('Failed to load records: ' + error, 'danger');
        }
    });
}

function renderRecords(records) {
    var tbody = $('#recordsTable tbody');
    tbody.empty();
    
    if (records && records.length > 0) {
        records.forEach(function(record) {
            // Handle both uppercase and lowercase property names
            var name = record.name || record.Name || '';
            var type = record.type || record.Type || 'A';
            var data = record.data || record.Data || '';
            var zone = record.zone || record.Zone || '';
            var ttl = record.ttl || record.Ttl || 3600;
            var priority = record.priority || record.Priority || 0;
            var enabled = record.enabled !== undefined ? record.enabled : (record.Enabled !== undefined ? record.Enabled : true);
            
            var statusBadge = enabled 
                ? '<span class="badge bg-success">Active</span>' 
                : '<span class="badge bg-secondary">Disabled</span>';
            
            var priorityDisplay = (type === 'MX' || type === 'SRV') && priority > 0 ? priority : '-';
            
            tbody.append(`
                <tr>
                    <td><code>${name}</code></td>
                    <td><span class="badge bg-info">${type}</span></td>
                    <td><code>${data}</code></td>
                    <td><span class="badge bg-primary">${zone}</span></td>
                    <td>${ttl}</td>
                    <td>${priorityDisplay}</td>
                    <td>${statusBadge}</td>
                </tr>
            `);
        });
    } else {
        tbody.html('<tr><td colspan="7" class="text-center text-muted">No DNS records</td></tr>');
    }
}

function showMessage(message, type) {
    var alertClass = 'alert-' + type;
    var $message = $('#statusMessage');
    $message.removeClass('d-none alert-success alert-danger alert-warning alert-info')
            .addClass(alertClass)
            .html(`
                <svg width="16" height="16" fill="currentColor" viewBox="0 0 16 16" class="me-2">
                    <path d="M8 15A7 7 0 1 1 8 1a7 7 0 0 1 0 14zm0 1A8 8 0 1 0 8 0a8 8 0 0 0 0 16z"/>
                    <path d="M7.002 11a1 1 0 1 1 2 0 1 1 0 0 1-2 0zM7.1 4.995a.905.905 0 1 1 1.8 0l-.35 3.507a.552.552 0 0 1-1.1 0L7.1 4.995z"/>
                </svg>
                ${message}
            `);
    
    setTimeout(function() {
        $message.addClass('d-none');
    }, 5000);
}
