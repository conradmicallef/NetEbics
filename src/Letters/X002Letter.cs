namespace NetEbics.Letters
{
    public class X002Letter : Letter
    {
        public override string Title => "Authentication certificate letter";
        public override string CertTitle => "Authentication certificate";
        public override string HashTitle => "Signature of Authentication certificate";
        public override string Version => "X002";

    }
}
