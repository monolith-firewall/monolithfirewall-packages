// IPsec Configuration JavaScript

$(document).ready(function() {
    loadSettings();
    loadConnections();

    $('#settingsForm').on('submit', function(e) {
        e.preventDefault();
        saveSettings();
    });

    $('#addConnectionBtn').on('click', function() {
        // TODO: Open add connection modal
        showMessage('Add connection functionality coming soon', 'info');
    });
});

function loadSettings() {
    $.ajax({
        url: '/api/packages/monolith-vpn/modules/ipsec/get-settings?_=' + new Date().getTime(),
        method: 'GET',
        cache: false,
        success: function(response) {
            if ((response.success || response.Success) && (response.data || response.Data)) {
                var settings = response.data || response.Data;
                $('#ipsecEnabled').prop('checked', settings.enabled || settings.Enabled || false);
                $('#mode').val(settings.mode || settings.Mode || 'transport');
                $('#natTraversal').prop('checked', settings.natTraversal !== undefined ? (settings.natTraversal || settings.NatTraversal) : true);
                $('#deadPeerDetection').prop('checked', settings.deadPeerDetection !== undefined ? (settings.deadPeerDetection || settings.DeadPeerDetection) : true);
                $('#dpdInterval').val(settings.deadPeerDetectionInterval || settings.DeadPeerDetectionInterval || 30);
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
        enabled: $('#ipsecEnabled').is(':checked'),
        mode: $('#mode').val(),
        natTraversal: $('#natTraversal').is(':checked'),
        deadPeerDetection: $('#deadPeerDetection').is(':checked'),
        deadPeerDetectionInterval: parseInt($('#dpdInterval').val()) || 30,
        logLevel: $('#logLevel').val()
    };

    $.ajax({
        url: '/api/packages/monolith-vpn/modules/ipsec/update-settings',
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

function loadConnections() {
    $.ajax({
        url: '/api/packages/monolith-vpn/modules/ipsec/get-connections?_=' + new Date().getTime(),
        method: 'GET',
        cache: false,
        success: function(response) {
            if ((response.success || response.Success) && (response.data || response.Data)) {
                var connections = response.data || response.Data || [];
                renderConnections(connections);
            }
        },
        error: function(xhr, status, error) {
            showMessage('Failed to load connections: ' + error, 'danger');
        }
    });
}

function renderConnections(connections) {
    var tbody = $('#connectionsTable tbody');
    tbody.empty();

    if (connections.length === 0) {
        tbody.append('<tr><td colspan="6" class="text-center text-muted">No connections configured</td></tr>');
        return;
    }

    connections.forEach(function(conn) {
        var statusBadge = '<span class="badge bg-' + (conn.status === 'up' || conn.Status === 'up' ? 'success' : 'secondary') + '">' + 
                         (conn.status || conn.Status || 'down') + '</span>';
        var row = '<tr>' +
            '<td>' + (conn.name || conn.Name || '') + '</td>' +
            '<td>' + (conn.type || conn.Type || '') + '</td>' +
            '<td>' + (conn.localAddress || conn.LocalAddress || '') + '</td>' +
            '<td>' + (conn.remoteAddress || conn.RemoteAddress || '') + '</td>' +
            '<td>' + statusBadge + '</td>' +
            '<td>' +
            '<button class="btn btn-sm btn-outline-primary me-1" onclick="startConnection(\'' + (conn.id || conn.Id) + '\')">Start</button>' +
            '<button class="btn btn-sm btn-outline-danger" onclick="stopConnection(\'' + (conn.id || conn.Id) + '\')">Stop</button>' +
            '</td>' +
            '</tr>';
        tbody.append(row);
    });
}

function startConnection(id) {
    $.ajax({
        url: '/api/packages/monolith-vpn/modules/ipsec/start-connection?id=' + id,
        method: 'GET',
        cache: false,
        success: function(response) {
            if (response.success || response.Success) {
                showMessage('Connection started', 'success');
                loadConnections();
            } else {
                showMessage('Failed to start connection', 'danger');
            }
        },
        error: function() {
            showMessage('Failed to start connection', 'danger');
        }
    });
}

function stopConnection(id) {
    $.ajax({
        url: '/api/packages/monolith-vpn/modules/ipsec/stop-connection?id=' + id,
        method: 'GET',
        cache: false,
        success: function(response) {
            if (response.success || response.Success) {
                showMessage('Connection stopped', 'success');
                loadConnections();
            } else {
                showMessage('Failed to stop connection', 'danger');
            }
        },
        error: function() {
            showMessage('Failed to stop connection', 'danger');
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
