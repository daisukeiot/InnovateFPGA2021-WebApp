// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

function toggleEvent(item) {

    var expanded = $(item).closest('.accordian-toggle').attr('aria-expanded');

    console.log("Expanded " + expanded);
    if (expanded == 'false' || expanded == undefined)
    {
        $(item).removeClass('fa-angle-double-down');
        $(item).addClass('fa-angle-double-up');
    }
    else {
        $(item).removeClass('fa-angle-double-up');
        $(item).addClass('fa-angle-double-down');
    }
}

function IoTHubModalClear() {
    $('#deviceConnectionState').html("");
    $('#deviceModelId').html("");
    $('#deviceConnectionString').html("");
    $('#deviceKey').html("");
    $('#newDeviceId').val("");
}

//
// Gets list of IoT Hub Devices
//
var IoTHubDeviceListGet = function (selectValue) {
    console.log("Getting Device List");
    $.ajax({
        type: "GET",
        url: '/home/IoTHubDeviceListGet',
        data: {},
        success: function (response) {
            $('#iotHubDeviceList').empty();
            $('#iotHubDeviceList').append(response);
            if (selectValue != null) {
//                $(`#iotHubDeviceList option[value='${selectValue}']`).prop('selected', true);
                $('#iotHubDeviceList').val(selectValue).change();
            }
        },
        error: function (jqXHR) {
            alert(" Status: " + jqXHR.status + " " + jqXHR.responseText);
        }
    });
}

//
// Gets device information.  Connection String, Model ID, Primary/Secondary Keys, and connect/disconnect status
//
function IoTHubGetDeviceInfo(deviceId, iothubname) {
    console.log("Getting Device Info : " + deviceId);
    $.ajax({
        type: "GET",
        url: '/home/IoTHubGetDeviceInfo',
        data: { deviceId: deviceId },
        success: function (response) {
            $('#deviceConnectionState').html(response.connectionState);

            if (response.connectionState == "Disconnected") {
                $('#deviceConnectionState').css("color", 'red');
            }
            else {
                $('#deviceConnectionState').css("color", 'blue');
            }
            $('#deviceModelId').html(response.deviceModelId);
            $('#deviceConnectionString').html("HostName=" + iothubname + ";DeviceId=" + response.deviceId + ";SharedAccessKey=" + response.symmetricKey);
            $('#deviceKey').html(response.symmetricKey);

            return true;
        },
        error: function (jqXHR) {
            // clear all fields
            IoTHubModalClear();
            alert(" Status: " + jqXHR.status + " " + jqXHR.responseText);
            return false;
        }
    });
}

//
// Deletes Device from IoT Hub
//
var IoTHubDeviceDelete = function (deviceId) {
    console.log("Deleting Device " + deviceId);

    $.ajax({
        type: "DELETE",
        url: '/home/IoTHubDeviceDelete',
        data: { deviceId: deviceId },
        success: function (response) {
            IoTHubModalClear();
            IoTHubDeviceListGet(null);
        },
        error: function (jqXHR) {
            alert(" Status: " + jqXHR.status + " " + jqXHR.responseText);
        }
    });
}

//
// Add a new device to IoT Hub
//
var IoTHubDeviceCreate = function (deviceId, isEdge) {
    console.log("Creating Device " + deviceId + " IsEdge :" + isEdge);

    $.ajax({
        type: "POST",
        url: '/home/IoTHubDeviceCreate',
        data: { deviceId: deviceId, isEdge : isEdge},
        success: function () {
            //debugger
            IoTHubModalClear();
            IoTHubDeviceListGet(deviceId);
        //    $(`#iotHubDeviceList option[value='${deviceId}']`).prop('selected', true);
        //    $('#iotHubDeviceList').val(deviceId).change();
        },
        error: function (jqXHR) {
            alert(" Status: " + jqXHR.status + " " + jqXHR.responseText);
        }
    });
}

//
// Gets list of DPS Enrollments
//
var DpsEnrollmentListGet = function (selectValue) {
    console.log("Getting Enrollment List");
    $.ajax({
        type: "GET",
        url: '/home/DpsEnrollmentListGet',
        data: {},
        success: function (response) {
            $("#dpsEnrollmentList").empty();
            $("#dpsEnrollmentList").append(response);
            if (selectValue != null) {
                $('#dpsEnrollmentList').val(selectValue).change();
            }

        },
        error: function (jqXHR) {
            alert(" Status: " + jqXHR.status + " " + jqXHR.responseText);
        }
    });
}

//
// Gets device information.  Connection String, Model ID, Primary/Secondary Keys, and connect/disconnect status
//
function DpsEnrollmentGet(enrollmentName) {
    console.log("Getting Enrollment Info : " + enrollmentName);
    var isGroup = $('#dpsEnrollmentList option:selected').attr("data-isGroup");
    $.ajax({
        type: "POST",
        url: '/home/DpsEnrollmentGet',
        data: { enrollmentName: enrollmentName, isGroup: isGroup },
        success: function (response) {
            var enrollmentTypeString = "";

            if (response.isGroup) {
                enrollmentTypeString = enrollmentTypeString + "Group Enrollment"
            }
            else {
                enrollmentTypeString = enrollmentTypeString + "Individual Enrollment"
            }

            if (response.isEdge) {
                enrollmentTypeString = enrollmentTypeString + " (IoT Edge)"
            }

            $('#dpsEnrollmentType').html(enrollmentTypeString);
            $('#dpsKey').html(response.symmetricKey);
            return true;
        },
        error: function (jqXHR) {
            // clear all fields
            alert(" Status: " + jqXHR.status + " " + jqXHR.responseText);
            return false;
        }
    });
}

//
// Deletes an enrollment from DPS
//
var DpsEnrollmentDelete = function (enrollmentName, isGroup) {
    console.log("Delete DPS Enrollment : " + enrollmentName + " IsGroup : " + isGroup);

    $.ajax({
        type: "DELETE",
        url: '/home/DpsEnrollmentDelete',
        data: { registrationId: enrollmentName, isGroup: isGroup },
        success: function (response) {
            //DpsEnrollmentListGet(null);
            $("#dpsEnrollmentList").empty();
            $("#dpsEnrollmentList").append(response);
            return true;
        },
        error: function (jqXHR) {
            // clear all fields
            alert(" Status: " + jqXHR.status + " " + jqXHR.responseText);
            return false;
        }
    });
}

//
// Create a new enrollment 
//
var DpsEnrollmentCreate = function (enrollmentName, isGroup, isEdge) {
    console.log("Create DPS Enrollment : " + enrollmentName + " IsGroup : " + isGroup);

    $.ajax({
        type: "POST",
        url: '/home/DpsEnrollmentCreate',
        data: { newRegistrationId: enrollmentName, isGroup: isGroup, isEdge: isEdge },
        success: function (response) {
            $("#dpsEnrollmentList").empty();
            $("#dpsEnrollmentList").append(response);
            $('#dpsEnrollmentList').val(enrollmentName).change();
            return true;
        },
        error: function (jqXHR) {
            // clear all fields
            alert(" Status: " + jqXHR.status + " " + jqXHR.responseText);
            return false;
        }
    });
}
