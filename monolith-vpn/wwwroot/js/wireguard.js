// WireGuard Configuration JavaScript

$(document).ready(function() {
    loadSettings();
    loadInterfaces();
    loadPeers();

    $('#settingsForm').on('submit', function(e) {
        e.preventDefault();
        saveSettings();
    });

    $('#addInterfaceBtn').on('click', function() {
        showMessage('Add interface functionality coming soon', 'info');
    });

    $('#addPeerBtn').on('click', function() {
        showMessage('Add peer functionality coming soon', 'info');
    });

    $('#interfaceFilter').on('change', function() {
        loadPeers();
    });
});

function loadSettings() {
    $.ajax({
        url: '/api/packages/monolith-vpn/modules/wireguard/get-settings?_=' + new Date().getTime(),
        method: 'GET',
        cache: false,
        success: function(response) {
            if ((response.success || response.Success) && (response.data || response.Data)) {
                var settings = response.data || response.Data;
                $('#wireguardEnabled').prop('checked', settings.enabled || settings.Enabled || false);
                $('#listenPort').val(settings.listenPort || settings.ListenPort || 51820);
                $('#forwardTraffic').prop('checked', settings.forwardTraffic !== undefined ? (settings.forwardTraffic || settings.ForwardTraffic) : false);
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
        enabled: $('#wireguardEnabled').is(':checked'),
        listenPort: parseInt($('#listenPort').val()) || 51820,
        forwardTraffic: $('#forwardTraffic').is(':checked'),
        logLevel: $('#logLevel').val()
    };

    $.ajax({
        url: '/api/packages/monolith-vpn/modules/wireguard/update-settings',
        method: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(settings),
        cache: false,
        success: function(response) {
            if (response.success || response.Success) {
                showMessage('Settings saved successfully', 'success');
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
        url: '/api/packages/monolith-vpn/modules/wireguard/get-interfaces?_=' + new Date().getTime(),
        method: 'GET',
        cache: false,
        success: function(response) {
            if ((response.success || response.Success) && (response.data || response.Data)) {
                var interfaces = response.data || response.Data || [];
                renderInterfaces(interfaces);
                updateInterfaceFilter(interfaces);
            }
        },
        error: function(xhr, status, error) {
            showMessage('Failed to load interfaces: ' + error, 'danger');
        }
    });
}

function loadPeers() {
    var interfaceId = $('#interfaceFilter').val();
    var url = '/api/packages/monolith-vpn/modules/wireguard/get-peers?_=' + new Date().getTime();
    if (interfaceId) {
        url += '&interface=' + interfaceId;
    }

    $.ajax({
        url: url,
        method: 'GET',
        cache: false,
        success: function(response) {
            if ((response.success || response.Success) && (response.data || response.Data)) {
                var peers = response.data || response.Data || [];
                renderPeers(peers);
            }
        },
        error: function(xhr, status, error) {
            showMessage('Failed to load peers: ' + error, 'danger');
        }
    });
}

function renderInterfaces(interfaces) {
    var tbody = $('#interfacesTable tbody');
    tbody.empty();

    if (interfaces.length === 0) {
        tbody.append('<tr><td colspan="6" class="text-center text-muted">No interfaces configured</td></tr>');
        return;
    }

    interfaces.forEach(function(iface) {
        var statusBadge = '<span class="badge bg-' + (iface.status === 'up' || iface.Status === 'up' ? 'success' : 'secondary') + '">' + 
                         (iface.status || iface.Status || 'down') + '</span>';
        var row = '<tr>' +
            '<td>' + (iface.name || iface.Name || '') + '</td>' +
            '<td>' + (iface.address || iface.Address || '') + '</td>' +
            '<td>' + (iface.listenPort || iface.ListenPort || '') + '</td>' +
            '<td>' + statusBadge + '</td>' +
            '<td>' + (iface.connectedPeers || iface.ConnectedPeers || 0) + '</td>' +
            '<td>' +
            '<button class="btn btn-sm btn-outline-primary me-1" onclick="startInterface(\'' + (iface.id || iface.Id) + '\')">Start</button>' +
            '<button class="btn btn-sm btn-outline-danger" onclick="stopInterface(\'' + (iface.id || iface.Id) + '\')">Stop</button>' +
            '</td>' +
            '</tr>';
        tbody.append(row);
    });
}

function renderPeers(peers) {
    var tbody = $('#peersTable tbody');
    tbody.empty();

    if (peers.length === 0) {
        tbody.append('<tr><td colspan="6" class="text-center text-muted">No peers configured</td></tr>');
        return;
    }

    peers.forEach(function(peer) {
        var statusBadge = '<span class="badge bg-' + (peer.status === 'connected' || peer.Status === 'connected' ? 'success' : 'secondary') + '">' + 
                         (peer.status || peer.Status || 'disconnected') + '</span>';
        var publicKey = peer.publicKey || peer.PublicKey || '';
        var shortKey = publicKey.length > 20 ? publicKey.substring(0, 20) + '...' : publicKey;
        var row = '<tr>' +
            '<td>' + (peer.name || peer.Name || '') + '</td>' +
            '<td>' + (peer.interfaceId || peer.InterfaceId || '') + '</td>' +
            '<td><code>' + shortKey + '</code></td>' +
            '<td>' + (peer.endpoint || peer.Endpoint || '') + '</td>' +
            '<td>' + statusBadge + '</td>' +
            '<td>' +
            '<button class="btn btn-sm btn-outline-danger" onclick="deletePeer(\'' + (peer.id || peer.Id) + '\')">Delete</button>' +
            '</td>' +
            '</tr>';
        tbody.append(row);
    });
}

function updateInterfaceFilter(interfaces) {
    var filter = $('#interfaceFilter');
    filter.empty();
    filter.append('<option value="">All Interfaces</option>');
    interfaces.forEach(function(iface) {
        filter.append('<option value="' + (iface.id || iface.Id) + '">' + (iface.name || iface.Name) + '</option>');
    });
}

function startInterface(id) {
    $.ajax({
        url: '/api/packages/monolith-vpn/modules/wireguard/start-interface?id=' + id,
        method: 'GET',
        cache: false,
        success: function(response) {
            if (response.success || response.Success) {
                showMessage('Interface started', 'success');
                loadInterfaces();
            } else {
                showMessage('Failed to start interface', 'danger');
            }
        },
        error: function() {
            showMessage('Failed to start interface', 'danger');
        }
    });
}

function stopInterface(id) {
    $.ajax({
        url: '/api/packages/monolith-vpn/modules/wireguard/stop-interface?id=' + id,
        method: 'GET',
        cache: false,
        success: function(response) {
            if (response.success || response.Success) {
                showMessage('Interface stopped', 'success');
                loadInterfaces();
            } else {
                showMessage('Failed to stop interface', 'danger');
            }
        },
        error: function() {
            showMessage('Failed to stop interface', 'danger');
        }
    });
}

function deletePeer(id) {
    if (!confirm('Are you sure you want to delete this peer?')) {
        return;
    }

    $.ajax({
        url: '/api/packages/monolith-vpn/modules/wireguard/delete-peer?id=' + id,
        method: 'GET',
        cache: false,
        success: function(response) {
            if (response.success || response.Success) {
                showMessage('Peer deleted', 'success');
                loadPeers();
            } else {
                showMessage('Failed to delete peer', 'danger');
            }
        },
        error: function() {
            showMessage('Failed to delete peer', 'danger');
        }
    });
}

function showMessage(message, type) {
    var alert = $('#statusMessage');
    alert.removeClass('d-none alert-success alert-danger alert-warning alert-info')
          .addClass('alert-' + type)
          .text(message);
    setTimeout(function() {
        alert.addClass('d-none');
    }, 5000);
}
