window.MultipleMediaField = window.MultipleMediaField || {};

MultipleMediaField.refreshValue = function (selectedField, valueInput) {
    var value = "";

    for (var n = 0; n < selectedField.options.length; n++) {
        var option = selectedField.options[n];
        value += (value !== "" ? "|" : "") + option.value;
    }

    valueInput.value = value;
}

MultipleMediaField.deleteCurrent = function (fieldId) {
    var selectedFieldId = "select#" + fieldId + "_selected";
    var selectedField = $sc(selectedFieldId);

    var selectedOptions = selectedField.children("option:selected");

    if (selectedOptions == undefined) {
        return;
    }

    if (selectedField.children().length === 1 && selectedOptions.length === 0) {
        selectedOptions = selectedField.children();
    } 

    var selectedValue = selectedOptions[0].value;
    
    selectedField = selectedField[0];

    if (selectedOptions.length > 0) {
        var index = selectedOptions.index();
        selectedOptions.remove();

        if (selectedField.length > 0) {
            index = Math.min(Math.max(--index, 0), selectedField.length - 1);
        }

        selectedField.selectedIndex = index;
    }

    var valueId = "#" + fieldId + "_Value";
    var valueInput = $sc(valueId);
    MultipleMediaField.refreshValue(selectedField, valueInput[0]);

    $sc(selectedField).parent().find("ul > li[data-item-id='" + selectedValue + "']").remove();

    $sc(selectedField).data('picker').destroy();
    $sc(selectedField).imagepicker();

    selectedField.focus();
}

MultipleMediaField.openCurrent = function (fieldId) {
    var selectedFieldId = "select#" + fieldId + "_selected";
    var selectedField = $sc(selectedFieldId);

    var selectedOptions = selectedField.children("option:selected");

    if (selectedOptions == undefined) {
        return;
    }

    if (selectedOptions.length > 0) {
        var id = selectedOptions[0].value;
        window.scForm.postRequest('', '', '', 'contenteditor:launchtab(url=' + id + ')');
    }
}

MultipleMediaField.moveUp = function(fieldId) {
    scContent.multilistMoveUp(fieldId);
    MultipleMediaField.reload(fieldId);
}

MultipleMediaField.moveDown = function (fieldId) {
    scContent.multilistMoveDown(fieldId);
    MultipleMediaField.reload(fieldId);
}

MultipleMediaField.reload = function (fieldId) {
    $sc("#" + fieldId + "_selected").data('picker').destroy();
    $sc("#" + fieldId + "_selected").imagepicker();
}

setTimeout(function () { $sc("select.image-picker").imagepicker(); }, 1000);