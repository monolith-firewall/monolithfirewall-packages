// OpenVPN Configuration JavaScript

$(document).ready(function() {
    loadSettings();
    loadServers();
    loadClients();

    $('#settingsForm').on('submit', function(e) {
        e.preventDefault();
        saveSettings();
    });

    $('#addServerBtn').on('click', function() {
        showMessage('Add server functionality coming soon', 'info');
    });

    $('#addClientBtn').on('click', function() {
        showMessage('Add client functionality coming soon', 'info');
    });
});

function loadSettings() {
    $.ajax({
        url: '/api/packages/monolith-vpn/modules/openvpn/get-settings?_=' + new Date().getTime(),
        method: 'GET',
        cache: false,
        success: function(response) {
            if ((response.success || response.Success) && (response.data || response.Data)) {
                var settings = response.data || response.Data;
                $('#openvpnEnabled').prop('checked', settings.enabled || settings.Enabled || false);
                $('#port').val(settings.port || settings.Port || 1194);
                $('#protocol').val(settings.protocol || settings.Protocol || 'udp');
                $('#cipher').val(settings.cipher || settings.Cipher || 'AES-256-GCM');
                $('#auth').val(settings.auth || settings.Auth || 'SHA256');
                $('#compression').prop('checked', settings.compression !== undefined ? (settings.compression || settings.Compression) : false);
            }
        },
        error: function(xhr, status, error) {
            showMessage('Failed to load settings: ' + error, 'danger');
        }
    });
}

function saveSettings() {
    var settings = {
        enabled: $('#openvpnEnabled').is(':checked'),
        port: parseInt($('#port').val()) || 1194,
        protocol: $('#protocol').val(),
        cipher: $('#cipher').val(),
        auth: $('#auth').val(),
        compression: $('#compression').is(':checked')
    };

    $.ajax({
        url: '/api/packages/monolith-vpn/modules/openvpn/update-settings',
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

function loadServers() {
    $.ajax({
        url: '/api/packages/monolith-vpn/modules/openvpn/get-servers?_=' + new Date().getTime(),
        method: 'GET',
        cache: false,
        success: function(response) {
            if ((response.success || response.Success) && (response.data || response.Data)) {
                var servers = response.data || response.Data || [];
                renderServers(servers);
            }
        },
        error: function(xhr, status, error) {
            showMessage('Failed to load servers: ' + error, 'danger');
        }
    });
}

function loadClients() {
    $.ajax({
        url: '/api/packages/monolith-vpn/modules/openvpn/get-clients?_=' + new Date().getTime(),
        method: 'GET',
        cache: false,
        success: function(response) {
            if ((response.success || response.Success) && (response.data || response.Data)) {
                var clients = response.data || response.Data || [];
                renderClients(clients);
            }
        },
        error: function(xhr, status, error) {
            showMessage('Failed to load clients: ' + error, 'danger');
        }
    });
}

function renderServers(servers) {
    var tbody = $('#serversTable tbody');
    tbody.empty();

    if (servers.length === 0) {
        tbody.append('<tr><td colspan="7" class="text-center text-muted">No servers configured</td></tr>');
        return;
    }

    servers.forEach(function(server) {
        var statusBadge = '<span class="badge bg-' + (server.status === 'up' || server.Status === 'up' ? 'success' : 'secondary') + '">' + 
                         (server.status || server.Status || 'down') + '</span>';
        var row = '<tr>' +
            '<td>' + (server.name || server.Name || '') + '</td>' +
            '<td>' + (server.interface || server.Interface || '') + '</td>' +
            '<td>' + (server.network || server.Network || '') + '</td>' +
            '<td>' + (server.port || server.Port || '') + '</td>' +
            '<td>' + statusBadge + '</td>' +
            '<td>' + (server.connectedClients || server.ConnectedClients || 0) + '</td>' +
            '<td>' +
            '<button class="btn btn-sm btn-outline-primary me-1" onclick="startServer(\'' + (server.id || server.Id) + '\')">Start</button>' +
            '<button class="btn btn-sm btn-outline-danger" onclick="stopServer(\'' + (server.id || server.Id) + '\')">Stop</button>' +
            '</td>' +
            '</tr>';
        tbody.append(row);
    });
}

function renderClients(clients) {
    var tbody = $('#clientsTable tbody');
    tbody.empty();

    if (clients.length === 0) {
        tbody.append('<tr><td colspan="6" class="text-center text-muted">No clients configured</td></tr>');
        return;
    }

    clients.forEach(function(client) {
        var statusBadge = '<span class="badge bg-' + (client.status === 'up' || client.Status === 'up' ? 'success' : 'secondary') + '">' + 
                         (client.status || client.Status || 'down') + '</span>';
        var row = '<tr>' +
            '<td>' + (client.name || client.Name || '') + '</td>' +
            '<td>' + (client.serverAddress || client.ServerAddress || '') + '</td>' +
            '<td>' + statusBadge + '</td>' +
            '<td>' + (client.localIp || client.LocalIp || '') + '</td>' +
            '<td>' + (client.remoteIp || client.RemoteIp || '') + '</td>' +
            '<td>' +
            '<button class="btn btn-sm btn-outline-primary me-1" onclick="startClient(\'' + (client.id || client.Id) + '\')">Start</button>' +
            '<button class="btn btn-sm btn-outline-danger" onclick="stopClient(\'' + (client.id || client.Id) + '\')">Stop</button>' +
            '</td>' +
            '</tr>';
        tbody.append(row);
    });
}

function startServer(id) {
    $.ajax({
        url: '/api/packages/monolith-vpn/modules/openvpn/start-server?id=' + id,
        method: 'GET',
        cache: false,
        success: function(response) {
            if (response.success || response.Success) {
                showMessage('Server started', 'success');
                loadServers();
            } else {
                showMessage('Failed to start server', 'danger');
            }
        },
        error: function() {
            showMessage('Failed to start server', 'danger');
        }
    });
}

function stopServer(id) {
    $.ajax({
        url: '/api/packages/monolith-vpn/modules/openvpn/stop-server?id=' + id,
        method: 'GET',
        cache: false,
        success: function(response) {
            if (response.success || response.Success) {
                showMessage('Server stopped', 'success');
                loadServers();
            } else {
                showMessage('Failed to stop server', 'danger');
            }
        },
        error: function() {
            showMessage('Failed to stop server', 'danger');
        }
    });
}

function startClient(id) {
    $.ajax({
        url: '/api/packages/monolith-vpn/modules/openvpn/start-client?id=' + id,
        method: 'GET',
        cache: false,
        success: function(response) {
            if (response.success || response.Success) {
                showMessage('Client started', 'success');
                loadClients();
            } else {
                showMessage('Failed to start client', 'danger');
            }
        },
        error: function() {
            showMessage('Failed to start client', 'danger');
        }
    });
}

function stopClient(id) {
    $.ajax({
        url: '/api/packages/monolith-vpn/modules/openvpn/stop-client?id=' + id,
        method: 'GET',
        cache: false,
        success: function(response) {
            if (response.success || response.Success) {
                showMessage('Client stopped', 'success');
                loadClients();
            } else {
                showMessage('Failed to stop client', 'danger');
            }
        },
        error: function() {
            showMessage('Failed to stop client', 'danger');
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
