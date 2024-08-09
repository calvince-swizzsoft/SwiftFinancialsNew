
$(document).ready(function () {
    $('.nav-link').on('click', function (e) {
        e.preventDefault();

        console.log(this);

        var target = $(this).attr('href');

        console.log(target);

        $('.tab-pane').removeClass('active');
        $(target).addClass('active');

        $('.nav-link').removeClass('active');
        $(this).addClass('active');
    });
});
