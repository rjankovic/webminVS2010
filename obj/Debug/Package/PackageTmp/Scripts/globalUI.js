/*
All the M2NMapping Fields (which have their ID set to one with the suffix of "_M2NIN"and "_M2NOUT" upon render) will, after a double-click on one of thier items,
transfer this item to the other select box in the pair. The "IN" items must be selected upon form submittion so that their values can be retrieved from the form
using Reques(Form[...]).
Also calls datepicker and text editor extenders (for inputs identified by a class).
*/

$(function () {

    $("select[id$='_M2NIN']").each(
    function () {
        M2NShift(this.id.toString(), this.id.replace("_M2NIN", "_M2NOUT"));
    });

    function M2NShift(a, b) {
        $('#' + a + ' option').each(function () {
            $(this).attr('selected', 'selected');
        });
        M2NShiftOneWay(a, b, a);
        M2NShiftOneWay(b, a, a);
    }

    function M2NShiftOneWay(a, b, inList) {
        $('#' + a + ' option').on('dblclick', function () {
            id = $(this).val();
            oznaceni = $(this).text();
            exists = false;
            $('#' + b + ' option').each(function () {
                if (id == $(this).val()) {
                    exists = true;
                    return;
                }
            });
            if (!exists) {
                $(this).removeAttr('selected');
                $('#' + b).append($(this).clone());
                $(this).remove();
            }
            $('#' + inList + ' option').each(function () {
                $(this).attr('selected', 'selected');
            });
        });

    }

    $(".includeDatePicker").each(
    function () {
        $(this).datepicker();
    });

    $(".includeEditor").each(
    function () {
        $(this).jqte();
    });

});