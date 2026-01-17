// DHCP Configuration JavaScript with Tabs

$(document).ready(function() {
    // Load settings
    loadSettings();
    
    // Load interfaces
    loadInterfaces();
    
    // Load leases
    loadLeases();

    // Handle settings form submission
    $('#settingsForm').on('submit', function(e) {
        e.preventDefault();
        saveSettings();
    });

    // Handle reset settings button
    $('#resetSettingsBtn').on('click', function() {
        loadSettings();
    });

    // Handle refresh leases button
    $('#refreshLeasesBtn').on('click', function() {
        loadLeases();
    });
});

function loadSettings() {
    $.ajax({
        url: '/api/packages/monolith-network/modules/dhcp/get-settings?_=' + new Date().getTime(),
        method: 'GET',
        cache: false,
        headers: {
            'X-Requested-With': 'XMLHttpRequest'
        },
        success: function(response) {
            // Handle both uppercase and lowercase property names
            if ((response.success || response.Success) && (response.data || response.Data)) {
                var settings = response.data || response.Data;
                // Removed dhcpEnabled - controlled via interface enable/disable
                $('#defaultLeaseTime').val(settings.defaultLeaseTime || settings.DefaultLeaseTime || 7200);
                $('#maxLeaseTime').val(settings.maxLeaseTime || settings.MaxLeaseTime || 86400);
                $('#dnsRegistration').prop('checked', settings.dnsRegistration || settings.DnsRegistration || false);
                $('#logLevel').val(settings.logLevel || settings.LogLevel || 'info');
            }
        },
        error: function(xhr, status, error) {
            showMessage('Failed to load settings: ' + error, 'danger');
        }
    });
}

function saveSettings() {
    var settings = {
        // Removed enabled - controlled via interface enable/disable
        defaultLeaseTime: parseInt($('#defaultLeaseTime').val()),
        maxLeaseTime: parseInt($('#maxLeaseTime').val()),
        dnsRegistration: $('#dnsRegistration').is(':checked'),
        logLevel: $('#logLevel').val()
    };

    $.ajax({
        url: '/api/packages/monolith-network/modules/dhcp/update-settings',
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

function loadInterfaces() {
    $.ajax({
        url: '/api/packages/monolith-network/modules/dhcp/get-interfaces?_=' + new Date().getTime(),
        method: 'GET',
        cache: false,
        headers: {
            'X-Requested-With': 'XMLHttpRequest'
        },
        success: function(response) {
            console.log('Interfaces response:', response);
            // Handle both uppercase and lowercase property names
            if ((response.success || response.Success) && (response.data || response.Data)) {
                renderInterfaces(response.data || response.Data);
            } else {
                console.error('Invalid response format:', response);
                showMessage('Failed to load interfaces: Invalid response format', 'danger');
            }
        },
        error: function(xhr, status, error) {
            console.error('AJAX error:', xhr, status, error);
            console.error('Response text:', xhr.responseText);
            showMessage('Failed to load interfaces: ' + (xhr.responseText || error), 'danger');
        }
    });
}

// Helper function to normalize interface object (handle both PascalCase and camelCase)
function normalizeInterface(iface) {
    return {
        name: iface.name || iface.Name || '',
        enabled: iface.enabled !== undefined ? iface.enabled : (iface.Enabled !== undefined ? iface.Enabled : false),
        subnet: iface.subnet || iface.Subnet || '',
        clientPolicy: iface.clientPolicy || iface.ClientPolicy || 'allow-all',
        poolStart: iface.poolStart || iface.PoolStart || '',
        poolEnd: iface.poolEnd || iface.PoolEnd || '',
        dns1: iface.dns1 || iface.Dns1 || '',
        dns2: iface.dns2 || iface.Dns2 || '',
        dns3: iface.dns3 || iface.Dns3 || '',
        dns4: iface.dns4 || iface.Dns4 || '',
        gateway: iface.gateway || iface.Gateway || '',
        domain: iface.domain || iface.Domain || '',
        leaseTime: iface.leaseTime || iface.LeaseTime || 7200,
        maxLeaseTime: iface.maxLeaseTime || iface.MaxLeaseTime || 86400,
        staticArp: iface.staticArp !== undefined ? iface.staticArp : (iface.StaticArp !== undefined ? iface.StaticArp : false)
    };
}

function renderInterfaces(interfaces) {
    var tabsHtml = '';
    var contentHtml = '';
    
    if (interfaces && interfaces.length > 0) {
        interfaces.forEach(function(ifaceRaw, index) {
            // Normalize interface object to handle PascalCase/camelCase
            var iface = normalizeInterface(ifaceRaw);
            
            var isActive = index === 0 ? 'active' : '';
            var isSelected = index === 0 ? 'true' : 'false';
            var isShow = index === 0 ? 'show active' : '';
            
            // Tab button
            tabsHtml += `
                <li class="nav-item" role="presentation">
                    <button class="nav-link ${isActive}" id="iface-${iface.name}-tab" 
                            data-bs-toggle="pill" data-bs-target="#iface-${iface.name}" 
                            type="button" role="tab" aria-controls="iface-${iface.name}" 
                            aria-selected="${isSelected}">
                        <span class="badge bg-${iface.enabled ? 'success' : 'secondary'} me-1"></span>
                        ${iface.name}
                    </button>
                </li>
            `;
            
            // Tab content
            contentHtml += `
                <div class="tab-pane fade ${isShow}" id="iface-${iface.name}" role="tabpanel" 
                     aria-labelledby="iface-${iface.name}-tab">
                    ${renderInterfaceConfig(iface)}
                </div>
            `;
        });
    } else {
        tabsHtml = '<li class="nav-item"><span class="text-muted">No interfaces available</span></li>';
    }
    
    $('#interfaceTabs').html(tabsHtml);
    $('#interfaceTabContent').html(contentHtml);
    
    // Attach event handlers
    $('.interface-config-form').on('submit', function(e) {
        e.preventDefault();
        saveInterfaceConfig($(this));
    });
}

function renderInterfaceConfig(iface) {
    return `
        <form class="interface-config-form" data-interface="${iface.name}">
            <div class="row mb-3">
                <label class="col-sm-3 col-form-label fw-bold">Enable</label>
                <div class="col-sm-9">
                    <div class="form-check form-switch">
                        <input class="form-check-input" type="checkbox" 
                               id="enabled-${iface.name}" 
                               ${iface.enabled ? 'checked' : ''}>
                        <label class="form-check-label" for="enabled-${iface.name}">
                            Enable DHCP server on ${iface.name} interface
                        </label>
                    </div>
                </div>
            </div>

            <div class="row mb-3">
                <label class="col-sm-3 col-form-label">Deny Unknown Clients</label>
                <div class="col-sm-9">
                    <select class="form-select" id="clientPolicy-${iface.name}">
                        <option value="allow-all" ${iface.clientPolicy === 'allow-all' ? 'selected' : ''}>Allow all clients</option>
                        <option value="allow-known-any" ${iface.clientPolicy === 'allow-known-any' ? 'selected' : ''}>Allow known clients from any interface</option>
                        <option value="allow-known-this" ${iface.clientPolicy === 'allow-known-this' ? 'selected' : ''}>Allow known clients from only this interface</option>
                    </select>
                    <div class="form-text">
                        Control which clients can receive DHCP addresses on this interface.
                    </div>
                </div>
            </div>

            <div class="row mb-3">
                <label class="col-sm-3 col-form-label">Subnet</label>
                <div class="col-sm-9">
                    <input type="text" class="form-control" id="subnet-${iface.name}" 
                           value="${iface.subnet || ''}" readonly>
                    <div class="form-text">Current subnet configuration for this interface.</div>
                </div>
            </div>

            <div class="row mb-3">
                <label class="col-sm-3 col-form-label fw-bold">Address Pool Range</label>
                <div class="col-sm-4">
                    <label class="form-label small">From</label>
                    <input type="text" class="form-control" id="poolStart-${iface.name}" 
                           value="${iface.poolStart || ''}" placeholder="192.168.1.100">
                </div>
                <div class="col-sm-5">
                    <label class="form-label small">To</label>
                    <input type="text" class="form-control" id="poolEnd-${iface.name}" 
                           value="${iface.poolEnd || ''}" placeholder="192.168.1.200">
                </div>
            </div>

            <div class="row mb-3">
                <label class="col-sm-3 col-form-label">DNS Servers</label>
                <div class="col-sm-9">
                    <input type="text" class="form-control mb-2" id="dns1-${iface.name}" 
                           value="${iface.dns1 || ''}" placeholder="8.8.8.8">
                    <input type="text" class="form-control mb-2" id="dns2-${iface.name}" 
                           value="${iface.dns2 || ''}" placeholder="8.8.4.4">
                    <input type="text" class="form-control mb-2" id="dns3-${iface.name}" 
                           value="${iface.dns3 || ''}" placeholder="DNS Server 3">
                    <input type="text" class="form-control" id="dns4-${iface.name}" 
                           value="${iface.dns4 || ''}" placeholder="DNS Server 4">
                </div>
            </div>

            <div class="row mb-3">
                <label class="col-sm-3 col-form-label">Gateway</label>
                <div class="col-sm-9">
                    <input type="text" class="form-control" id="gateway-${iface.name}" 
                           value="${iface.gateway || ''}" placeholder="192.168.1.1">
                    <div class="form-text">
                        Default gateway for DHCP clients. Leave empty to use interface IP.
                    </div>
                </div>
            </div>

            <div class="row mb-3">
                <label class="col-sm-3 col-form-label">Domain Name</label>
                <div class="col-sm-9">
                    <input type="text" class="form-control" id="domain-${iface.name}" 
                           value="${iface.domain || ''}" placeholder="local.domain">
                    <div class="form-text">
                        Domain name provided to DHCP clients.
                    </div>
                </div>
            </div>

            <div class="row mb-3">
                <label class="col-sm-3 col-form-label">Default Lease Time</label>
                <div class="col-sm-9">
                    <input type="number" class="form-control" id="leaseTime-${iface.name}" 
                           value="${iface.leaseTime || 7200}" min="60">
                    <div class="form-text">Lease time in seconds (default: 7200).</div>
                </div>
            </div>

            <div class="row mb-3">
                <label class="col-sm-3 col-form-label">Maximum Lease Time</label>
                <div class="col-sm-9">
                    <input type="number" class="form-control" id="maxLeaseTime-${iface.name}" 
                           value="${iface.maxLeaseTime || 86400}" min="60">
                    <div class="form-text">Maximum lease time in seconds (default: 86400).</div>
                </div>
            </div>

            <div class="row mb-3">
                <label class="col-sm-3 col-form-label">Static ARP</label>
                <div class="col-sm-9">
                    <div class="form-check form-switch">
                        <input class="form-check-input" type="checkbox" 
                               id="staticArp-${iface.name}" 
                               ${iface.staticArp ? 'checked' : ''}>
                        <label class="form-check-label" for="staticArp-${iface.name}">
                            Enable Static ARP
                        </label>
                    </div>
                    <div class="form-text">
                        Restricts communication to only hosts with static mappings.
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
                <button type="button" class="btn btn-outline-secondary" onclick="loadInterfaces()">Reset</button>
            </div>
        </form>
    `;
}

function saveInterfaceConfig($form) {
    var ifaceName = $form.data('interface');
    
    var config = {
        interface: ifaceName,
        enabled: $(`#enabled-${ifaceName}`).is(':checked'),
        clientPolicy: $(`#clientPolicy-${ifaceName}`).val(),
        poolStart: $(`#poolStart-${ifaceName}`).val(),
        poolEnd: $(`#poolEnd-${ifaceName}`).val(),
        dns1: $(`#dns1-${ifaceName}`).val(),
        dns2: $(`#dns2-${ifaceName}`).val(),
        dns3: $(`#dns3-${ifaceName}`).val(),
        dns4: $(`#dns4-${ifaceName}`).val(),
        gateway: $(`#gateway-${ifaceName}`).val(),
        domain: $(`#domain-${ifaceName}`).val(),
        leaseTime: parseInt($(`#leaseTime-${ifaceName}`).val()),
        maxLeaseTime: parseInt($(`#maxLeaseTime-${ifaceName}`).val()),
        staticArp: $(`#staticArp-${ifaceName}`).is(':checked')
    };

    $.ajax({
        url: '/api/packages/monolith-network/modules/dhcp/update-interface',
        method: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(config),
        headers: {
            'X-Requested-With': 'XMLHttpRequest'
        },
        success: function(response) {
            // Handle both uppercase and lowercase property names
            if (response.success || response.Success) {
                showMessage(`Configuration saved for ${ifaceName}!`, 'success');
                // Reload interfaces to update status badges
                loadInterfaces();
            } else {
                showMessage('Failed to save configuration: ' + (response.error || response.Error || 'Unknown error'), 'danger');
            }
        },
        error: function(xhr, status, error) {
            showMessage('Failed to save configuration: ' + error, 'danger');
        }
    });
}

function loadLeases() {
    $.ajax({
        url: '/api/packages/monolith-network/modules/dhcp/get-leases?_=' + new Date().getTime(),
        method: 'GET',
        cache: false,
        headers: {
            'X-Requested-With': 'XMLHttpRequest'
        },
        success: function(response) {
            // Handle both uppercase and lowercase property names
            if ((response.success || response.Success) && (response.data || response.Data)) {
                renderLeases(response.data || response.Data);
            } else {
                // Empty leases - still render empty table
                renderLeases([]);
            }
        },
        error: function(xhr, status, error) {
            showMessage('Failed to load leases: ' + error, 'danger');
        }
    });
}

function renderLeases(leases) {
    var tbody = $('#leasesTable tbody');
    tbody.empty();
    
    if (leases && leases.length > 0) {
        leases.forEach(function(lease) {
            // Handle both uppercase and lowercase property names
            var ipAddress = lease.ipAddress || lease.IpAddress || '';
            var macAddress = lease.macAddress || lease.MacAddress || '';
            var hostname = lease.hostname || lease.Hostname || '<em class="text-muted">Unknown</em>';
            var iface = lease.interface || lease.Interface || '';
            var status = lease.status || lease.Status || lease.state || lease.State || 'active';
            var leaseStart = lease.leaseStart || lease.LeaseStart;
            var leaseEnd = lease.leaseEnd || lease.LeaseEnd;
            
            var statusBadge = status === 'active' 
                ? '<span class="badge bg-success">Active</span>' 
                : '<span class="badge bg-secondary">Expired</span>';
            
            tbody.append(`
                <tr>
                    <td><code>${ipAddress}</code></td>
                    <td><code>${macAddress}</code></td>
                    <td>${hostname}</td>
                    <td><span class="badge bg-primary">${iface}</span></td>
                    <td>${leaseStart ? new Date(leaseStart).toLocaleString() : '-'}</td>
                    <td>${leaseEnd ? new Date(leaseEnd).toLocaleString() : '-'}</td>
                    <td>${statusBadge}</td>
                </tr>
            `);
        });
    } else {
        tbody.html('<tr><td colspan="7" class="text-center text-muted">No active leases</td></tr>');
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
