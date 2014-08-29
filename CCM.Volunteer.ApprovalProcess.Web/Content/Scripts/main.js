//Query string plug in
(function ($) {
    $.QueryString = (function (a) {
        if (a == "") return {};
        var b = {};
        for (var i = 0; i < a.length; ++i) {
            var p = a[i].split('=');
            if (p.length != 2) continue;
            b[p[0]] = decodeURIComponent(p[1].replace(/\+/g, " "));
        }
        return b;
    })(window.location.search.substr(1).split('&'))
})(jQuery);

$(document).ready(function () {


    $('#selectList').selectpicker();
    $('#reasonList').selectpicker();
    //$('#howwelldoyouknow').selectpicker();
    //$('#wouldyourecommend').selectpicker();
    //$('#trustwithchildren').selectpicker();
    $('#placeInMinistry').selectpicker();
    $('.datepicker').datepicker()

    $('span.timeago').timeago();

    $('#selectList').on('change', function () {
        if (this.value == 1) {
            $('#reasons').hide();
        }
        else {
            $('#reasons').show();
        }


        if (this.value == 4) {
            $('#followUpDate').show();
        }
        else {
            $('#followUpDate').hide();
        }
    });


    // Validation
    $('#form').validate({
        errorPlacement: function (error, element) {
            error.insertBefore(element) + "</br>";
        },

        messages: {
            followupdate: "Please select a date"
        },

        submitHandler: function (form) {
            $(this).prop("disabled", true);

            //show loading
            $(".loading").show();

            console.log($("#form").serialize());
            $.post(getUrl(), $("#form").serialize(), function (data) {

                //hide loading
                $(".loading").hide();

                //$("#submitbutton").prop("disabled", false);

                //show success
                if (data.success) {
                    var pathArray = window.location.pathname.split('/');
                    var segment = pathArray[pathArray.length - 2];

                    //if this is the red flag page (other pages don't return notes field)
                    if (data.notes != null) {

                        var content = '<div id="redflag_' + data.contact.contact_ID + '" class="panel panel-default">' +
                            '<div class="offer ' + getredflagclass(data.contact.contact_ID) + '">' +
                                '<div class="shape">' +
                                    '<div class="shape-text">' +
                                        '<span class="glyphicon glyphicon-question-sign"></span>' +
                                    '</div>' +
                                '</div>' +
                                '<div class="offer-content">' +
                                    '<h3 class="lead">' +
                                        '<img src="https://images.calvaryccm.com/mp/image/contacts/' + data.contact.contact_ID + '.png?w=50&amp;h=50" class="pull-left"> ' + data.contact.nickname + ' ' + data.contact.last_Name +
                                        '<div class="small lead">Responded <span class="timeago">just now</span></div>' +
                                    '</h3>' +
                                    '<p>' + data.notes

                                    '</p>' +
                                '</div>' +
                            '</div>' +
                        '</div>';

                        //If the person chooses approve: get the id of the person, get their notes, get their selection option and add them to the red flag notes section




                        //if this person already has voted
                        if ($("#redflag_" + data.contact.contact_ID).get(0))
                            $("#redflag_" + data.contact.contact_ID).replaceWith(content);
                            //otherwise 
                        else {
                            //if($("")){
                            $(".redFlagNotes").append(content)
                        }
                            
                    }

                    $("#success-message").show();
                }
                else
                    $("#error-message").show();

                $("#form").hide();
            }).fail(function (error) {
                $(".loading").hide();
                $("#error-message").show();
            });
            errorLabelContainer: '#errors';
        },

        rules: {
            'followupdate': {
                required: function () {
                    return $('#followUpDate').is(':visible');
                }
            }
        }
    });



    $('#referenceCheckForm').validate({
        errorPlacement: function (error, element) {
            error.insertBefore(element) + "</br>";
        },

        messages: {
            howwelldoyouknow: "Please select an option",
            wouldyourecommend: "Please select an option",
            trustwithchildren: "Please select an option"
        },

        submitHandler: function (form) {
            //prevent default postback
            //e.preventDefault();
            $(this).prop("disabled", true);

            //show loading
            $(".loading").show();

            var pathArray = window.location.pathname.split('/');
            console.log($("#referenceCheckForm").serialize());
            $.post(getUrl(), $("#referenceCheckForm").serialize(), function (data) {

                //hide loading
                $(".loading").hide();

                //$("#submitbutton").prop("disabled", false);

                //show success
                if (data.success)
                    $("#success-message").show();
                else
                    $("#error-message").show();

                $("#referenceCheckForm").hide();
            })
              .fail(function (error) {
                  $("#error-message").show();
              });
        },
        errorLabelContainer: '#errors'

    });

    $("img.lazy").lazyload({
        effect: "fadeIn"
    });

});

function getUrl() {

    var pathArray = window.location.pathname.split('/');



    switch (pathArray[pathArray.length - 2]) {
        case "red-flag/":
        case "red-flag":

            return window.applicationBaseUrl + "red-flag/" + pathArray[pathArray.length - 1] + "?cid=" + $.QueryString.cid;

        case "reference-check/":
        case "reference-check":

            return window.applicationBaseUrl + "reference-check/" + pathArray[pathArray.length - 1];

        case "approve-deny/":
        case "approve-deny":

            return window.applicationBaseUrl + "approve-deny/" + pathArray[pathArray.length - 1];

        case "place-volunteer/":
        case "place-volunteer":

            return window.applicationBaseUrl + "place-volunteer/" + pathArray[pathArray.length - 1] + "?id=" + $.QueryString.id;

        case "return-to-director/":
        case "return-to-director":

            return window.applicationBaseUrl + "return-to-director/" + pathArray[pathArray.length - 1] + "?id=" + $.QueryString.id + "&from=" + $.QueryString.from;

        default:

            break;
    }
}

function getredflagclass(contactid)
{
    //look up on the page which is option was selected
    //var redFlagVote = new Number($('#selectList').id);//get the currently selected option 

    var redFlagVote = $("#selectList").val();

    //var redFlagVote = $("#selectList").change(function(){
      //  $(this[this.selectedIndex]).val();
    //});


    var approved = new Number($('#approvedVotes').text());
    var hold = new Number($('#holdVotes').text());
    var problem = new Number($('#problemVotes').text());
    var element = $("#redflag_" + contactid + " div:first-child");


    //if approve (can also do a switch statement here)
    if (redFlagVote == 1) {
        //if this already exists
        if ($("#redflag_" + contactid).get(0))
        {
            //if element has this class
            if (element.hasClass("offer-warning")){
                hold--;
                $("#holdVotes").html(hold);
            }
            else if (element.hasClass("offer-danger")) {
                problem--;

                $("#problemVotes").html(problem);
            }
        }
        approved++;
        
        $("#approvedVotes").html(approved);
        return "offer-success";
    } 
    else if (redFlagVote == 2) {
        //if element has this class
        if (element.hasClass("offer-success")){
            approved--;
            $("#approvedVotes").html(approved);
        }
        else if (element.hasClass("offer-danger")) {
            problem--;
            $("#problemVotes").html(problem);
        }
        hold++;
        
        $("#holdVotes").html(hold);
        return "offer-warning";        
    }


    else if (redFlagVote == 3) {
        //if element has this class
        if (element.hasClass("offer-success")) {
            approved--;
            $("#approvedVotes").html(approved);
        }
        else if (element.hasClass("offer-hold")) {
            hold--;
            $("#holdVotes").html(hold);
        }
        problem++;
        $("#problemVotes").html(problem);
        return "offer-danger";
        
    }
    else{
        return "offer-primary";
    }

    //increment number at top
    //return the specific css class as a string
    //else if hold
    //return the specific css class as a string
}
