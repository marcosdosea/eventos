$(document).ready(function () {
	$('#tablePessoa').DataTable({
		order: [[1, 'asc']],
		columnDefs: [
			{ orderable: true, targets: [1] },
			{ orderable: false, targets: [0,2] }
		],
		searching: true,
		ordering: true,
		paging: true,
		language: {
			url: '/lib/datatable/js/pt-BR.json'
		}
	});

    if ($('#tableGestorEventListar').length) {
        $('#tableGestorEventListar').DataTable({
            columnDefs: [
                { orderable: true, targets: [0, 1, 2, 3, 4] },
                { orderable: false, targets: [5] }
            ],
            order: [[1, 'desc']],
            searching: true,
            ordering: true,
            paging: true,
            pageLength: 10,
            language: {
                url: '/lib/datatable/js/pt-BR.json'
            }
        });
    }
});