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
			url: '@Url.Content("~/lib/datatable/js/pt-BR.json")'
		}
	});
});