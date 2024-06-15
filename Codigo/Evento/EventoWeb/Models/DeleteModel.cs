namespace EventoWeb.Models
{
    public class DeleteModel
    {
        public string ModalId { get; set; }
        public string ActionName { get; set; }
        public string ControllerName { get; set; }
        public object RouteValues { get; set; }
        public string ConfirmMessage { get; set; }
        public string ItemName { get; set; }
    }

}
