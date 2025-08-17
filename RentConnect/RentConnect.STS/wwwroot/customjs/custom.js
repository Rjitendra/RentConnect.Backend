$(function () {
    $('#termsLink').click(function (event) {
        event.preventDefault(); //
        $.ajax({
            url: 'https://lordhood.me/STS/TermAndCondition.pdf', // url: '/TermAndCondition.pdf',
            method: 'GET',
            xhrFields: {
                responseType: 'blob'
            },
            success: function (data) {
                var a = document.createElement('a');
                var url = window.URL.createObjectURL(data);
                a.href = url;
                a.download = 'TermAndCondition.pdf';
                $('body').append(a);
                a.click();
                setTimeout(function () {
                    $(a).remove();
                    window.URL.revokeObjectURL(url);
                }, 0);
            },
            error: function (error) {
                console.log(error);
            }
        });
    });

    $('#countryCode').on('shown.bs.select', function () {
        $(this).appendTo('body');
    });
    $('#searchBox').autocomplete({
        appendTo: 'body',
        source: function (request, response) {
            var query = request.term;
            var apiKey = "-ljoqanEokmGQGzEhFJVzQ39085";

            var data = {
                all: false,
                template: "{formatted_address}{postcode,, }{postcode}",
                top: 6,
                fuzzy: false
            };

            if (query.length > 3) {
                $('#searchBox').addClass('address-loader'); // add the "loading" class to the textbox
                $.ajax({
                    url: `https://api.getAddress.io/autocomplete/${query}?api-key=${apiKey}`,
                    type: 'POST',
                    contentType: 'application/json', // Set content type to JSON
                    data: JSON.stringify(data), // Convert data to JSON string

                    success: function (data) {
                        $('#searchBox').removeClass('address-loader'); // remove the "loading" class from the textbox
                        var suggestions = data.suggestions.map(result => {
                            return {
                                label: result.address,
                                value: result.id
                            };
                        });
                        response(suggestions);
                    },
                    error: function (error) {
                        $('#searchBox').removeClass('address-loader'); // remove the "loading" class from the textbox
                        console.log(error);
                    }
                });
            }
        },
        select: function (event, ui) {
            event.preventDefault();
            $('#searchBox').val(ui.item.label);

            // Trigger your desired event here.
            var addressId = ui.item.value;
            var apiKey = "-ljoqanEokmGQGzEhFJVzQ39085";
            $.ajax({
                url: `https://api.getAddress.io/get/${addressId}?api-key=${apiKey}`,
                type: 'GET',
                success: function (data) {
                    // Handle the success response here
                    $('#postcode').val(data.postcode);
                },
                error: function (error) {
                    // Handle the error response here
                    console.log("Get address error:", error);
                }
            });
        }
    });
});