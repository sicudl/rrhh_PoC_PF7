using System;

using System.Text;
using System.Xml.Serialization;
using System.IO;
using System.Collections;
using System.Collections.Generic;

using System.Threading.Tasks;
using System.ServiceModel.Channels;

using System.Xml;

namespace ConsoleApp1
{
    /*
    internal class CustomTextMessageEncoderFactory : MessageEncoderFactory
    {
        private System.ServiceModel.Channels.MessageEncoder _encoder;
        private MessageVersion _version;
        private string _mediaType;
        private string _charSet;

        internal CustomTextMessageEncoderFactory(string mediaType, string charSet, MessageVersion version)
        {
            _version = version;
            _mediaType = mediaType;
            _charSet = charSet;
            _encoder = new CustomTextMessageEncoder(this);
        }

        public override System.ServiceModel.Channels.MessageEncoder Encoder
        {
            get
            {
                return _encoder;
            }
        }

        public override MessageVersion MessageVersion
        {
            get
            {
                return _version;
            }
        }

        internal string MediaType
        {
            get
            {
                return _mediaType;
            }
        }

        internal string CharSet
        {
            get
            {
                return _charSet;
            }
        }
    }
    internal class CustomTextMessageEncoder : System.ServiceModel.Channels.MessageEncoder
    {
        private CustomTextMessageEncoderFactory _factory;
        private XmlWriterSettings _writerSettings;
        private string _contentType;

        public override bool IsContentTypeSupported(string contentType)
        {
            return true;
        }
        public CustomTextMessageEncoder(CustomTextMessageEncoderFactory factory)
        {
            _factory = factory;

            _writerSettings = new XmlWriterSettings();
            _writerSettings.Encoding = Encoding.GetEncoding(factory.CharSet);
            _contentType = string.Format("{0}; charset={1}",
                _factory.MediaType, _writerSettings.Encoding.WebName);
        }

        public override string ContentType
        {
            get
            {
                return _contentType;
            }
        }

        public override string MediaType
        {
            get
            {
                return _factory.MediaType;
            }
        }

        public override MessageVersion MessageVersion
        {
            get
            {
                return _factory.MessageVersion;
            }
        }

        public override Message ReadMessage(Stream stream, int maxSizeOfHeaders, string contentType)
        {
            XmlReader reader = XmlReader.Create(stream);
            return Message.CreateMessage(reader, maxSizeOfHeaders, MessageVersion);
        }

        public override Message ReadMessage(ArraySegment<byte> buffer, BufferManager bufferManager, string contentType)
        {
            byte[] msgContents = new byte[buffer.Count];
            Array.Copy(buffer.Array, buffer.Offset, msgContents, 0, msgContents.Length);
            bufferManager.ReturnBuffer(buffer.Array);

            MemoryStream stream = new MemoryStream(msgContents);
            return ReadMessage(stream, int.MaxValue);
        }

        public override void WriteMessage(Message message, Stream stream)
        {
            using (XmlWriter writer = XmlWriter.Create(stream, _writerSettings))
            {
                message.WriteMessage(writer);
            }
        }

        public override ArraySegment<byte> WriteMessage(Message message, int maxMessageSize, BufferManager bufferManager, int messageOffset)
        {
            ArraySegment<byte> messageBuffer;
            byte[] writeBuffer = null;

            int messageLength;
            using (MemoryStream stream = new MemoryStream())
            {
                using (XmlWriter writer = XmlWriter.Create(stream, _writerSettings))
                {
                    message.WriteMessage(writer);
                }

                // TryGetBuffer is the preferred path but requires 4.6
                //stream.TryGetBuffer(out messageBuffer);
                writeBuffer = stream.ToArray();
                messageBuffer = new ArraySegment<byte>(writeBuffer);

                messageLength = (int)stream.Position;
            }

            int totalLength = messageLength + messageOffset;
            byte[] totalBytes = bufferManager.TakeBuffer(totalLength);
            Array.Copy(messageBuffer.Array, 0, totalBytes, messageOffset, messageLength);

            ArraySegment<byte> byteArray = new ArraySegment<byte>(totalBytes, messageOffset, messageLength);
            return byteArray;
        }
    }
    public class CustomTextMessageBindingElement : MessageEncodingBindingElement
    {
        private MessageVersion _msgVersion;
        private string _mediaType;
        private string _encoding;
        private XmlDictionaryReaderQuotas _readerQuotas;

        private CustomTextMessageBindingElement(CustomTextMessageBindingElement binding)
            : this(binding.Encoding, binding.MediaType, binding.MessageVersion)
        {
            _readerQuotas = new XmlDictionaryReaderQuotas();
            binding.ReaderQuotas.CopyTo(_readerQuotas);
        }

        public CustomTextMessageBindingElement(string encoding, string mediaType, MessageVersion msgVersion)
        {
            if (encoding == null)
                throw new ArgumentNullException("encoding");

            if (mediaType == null)
                throw new ArgumentNullException("mediaType");

            if (msgVersion == null)
                throw new ArgumentNullException("msgVersion");

            _msgVersion = msgVersion;
            _mediaType = mediaType;
            _encoding = encoding;
            _readerQuotas = new XmlDictionaryReaderQuotas();
        }

        public CustomTextMessageBindingElement(string encoding, string mediaType)
    : this(encoding, mediaType, System.ServiceModel.Channels.MessageVersion.Soap12WSAddressing10)
        {
        }

        public CustomTextMessageBindingElement(string encoding)
            : this(encoding, "text/xml")
        {
        }

        public override MessageVersion MessageVersion
        {
            get
            {
                return _msgVersion;
            }

            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");
                _msgVersion = value;
            }
        }

        public string MediaType
        {
            get
            {
                return _mediaType;
            }

            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");
                _mediaType = value;
            }
        }

        public string Encoding
        {
            get
            {
                return _encoding;
            }

            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");
                _encoding = value;
            }
        }

        // This encoder does not enforces any quotas for the unsecure messages. The 
        // quotas are enforced for the secure portions of messages when this encoder
        // is used in a binding that is configured with security. 
        public XmlDictionaryReaderQuotas ReaderQuotas
        {
            get
            {
                return _readerQuotas;
            }
        }

        public override MessageEncoderFactory CreateMessageEncoderFactory()
        {
            return new CustomTextMessageEncoderFactory(MediaType,
                Encoding, MessageVersion);
        }

        public override BindingElement Clone()
        {
            return new CustomTextMessageBindingElement(this);
        }

        public override IChannelFactory<TChannel> BuildChannelFactory<TChannel>(BindingContext context)
        {
            if (context == null)
                throw new ArgumentNullException("context");

            context.BindingParameters.Add(this);
            return context.BuildInnerChannelFactory<TChannel>();
        }

        public override bool CanBuildChannelFactory<TChannel>(BindingContext context)
        {
            if (context == null)
                throw new ArgumentNullException("context");

            return context.CanBuildInnerChannelFactory<TChannel>();
        }

        public override IChannelListener<TChannel> BuildChannelListener<TChannel>(BindingContext context)
        {
            if (context == null)
                throw new ArgumentNullException("context");

            context.BindingParameters.Add(this);
            return context.BuildInnerChannelListener<TChannel>();
        }

        public override bool CanBuildChannelListener<TChannel>(BindingContext context)
        {
            if (context == null)
                throw new ArgumentNullException("context");

            context.BindingParameters.Add(this);
            return context.CanBuildInnerChannelListener<TChannel>();
        }

        public override T GetProperty<T>(BindingContext context)
        {
            if (typeof(T) == typeof(XmlDictionaryReaderQuotas))
            {
                return (T)(object)_readerQuotas;
            }

            return base.GetProperty<T>(context);
        }
    }
    */
    /// <summary>
    /// PoC per a PF v7 C# .NET > 4.5 (por TLS >1.2)
    /// Visual Studio > 2015 (por TLS >1.2)
    ///
    /// La captura del WSDL ok. Compilación tipos bien.
    /// instàncias i uso métodos bien
    /// llamada salta excepción en client por el encoding inesperado de la respuesta
    ///
    /// la captura del WSDL bé, compile bé, ús de mètodes i variables del WSDL bé,...però :
    /// bug, la resposta arribe malament i fa petar a .NET
    ///
    /// https://stackoverflow.com/questions/41684342/the-content-type-text-xml-charset-utf-8-of-the-response-message-does-not-matc/41708315
    /// https://stackoverflow.com/questions/1908030/calling-a-webservice-that-uses-iso-8859-1-encoding-from-wcf
    /// 
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {



            ArrayList DNISignants = new ArrayList();
            DNISignants.Add("78080428M");
            DNISignants.Add("40893499M");
            string DNI1 = "43716052K";
            string DNI2 = "";

            string RespostaFinalRequestID = "";
            ArrayList LlistaSignants = new ArrayList();
            foreach (string Signant in DNISignants)
            {
                PF7.user Usuari1 = new PF7.user();
                Usuari1.identifier = Signant;
                Usuari1.name = "";
                Usuari1.surname1 = "";
                Usuari1.surname2 = "";

                LlistaSignants.Add(Signant);
            }

            if (DNI2.Length > 0) LlistaSignants.Add(DNI2);
            if (DNI1.Length > 0) LlistaSignants.Add(DNI1);


            PF7.document docPerSignar = new PF7.document();
            docPerSignar.identifier = "document de prova";
            //"CASCADA con 2 lineas firmantes PDF_" + System.DateTime.Now.Ticks.ToString();

            docPerSignar.name = "Nom document";// "DocumentProva.pdf";
            byte[] bytes = { 2, 4, 6, 8, 10, 12, 14, 16, 18, 20 };
            docPerSignar.content = File.ReadAllBytes("HOLA.PDF");

            //System.IO.File.ReadAllBytes(System.IO.Directory.GetCurrentDirectory() + "\\prova.pdf");
            docPerSignar.mime = "application/pdf";
            docPerSignar.sign = true;
            docPerSignar.signSpecified = true;

            docPerSignar.documentType = new PF7.documentType();
            docPerSignar.documentType.identifier = "GENERICO";
            //docPerSignar.documentType.valid = true;
            docPerSignar.uri = "application/pdf";
            docPerSignar.documentType.validSpecified = true;

            //docPerSignar.documentType.description = DescripcioDocument;// "PRUEBA capa SOA";
            PF7.document[] docsAsignar = new PF7.document[1];
            docsAsignar[0] = docPerSignar;

            PF7.request req = new PF7.request();
            req.application = "PFIRMA_PERSONAL";
            req.documentList = docsAsignar;
            req.reference = " de " + " és una prova simple";
            //"DIR3." + System.DateTime.Now.Millisecond.ToString() + "." + System.DateTime.Now.Year;
            if (req.reference.Length > 30)
                req.reference = req.reference.Substring(0, 30);

            req.subject = "DescripcioDocument";
            req.text = "DescripcioDocument";

            //"Texto comentario de la firma automatizada desde capa servicios";
            DateTime DataLimit = System.DateTime.Now;
            req.fentry = System.DateTime.Now;
            if (DataLimit != null)
                req.fexpiration = DataLimit;
            else
                req.fexpiration = System.DateTime.Now.AddDays(60);

            req.fexpirationSpecified = true;
            req.fstart = System.DateTime.Now.AddMinutes(1);
            req.fstartSpecified = true;
            req.importanceLevel = new PF7.importanceLevel();
            //req.importanceLevel.description = "Poco importante";
            req.importanceLevel.levelCode = "1";

            req.signLineList = new PF7.signLine[LlistaSignants.Count];
            int linea = 0;

            ArrayList ModoVistiPlauConfig = null;

            foreach (string strDNIsignant in LlistaSignants)
            {
                PF7.user signant = new PF7.user();
                signant.identifier = strDNIsignant;
                req.signLineList[linea] = new PF7.signLine();

                if (ModoVistiPlauConfig != null)
                {
                    //és un signant NO-càrrec?
                    if (strDNIsignant != DNI1 && strDNIsignant != DNI2)
                    {
                        //els N-2 signants del document compartit, son: o tots VISTO BUENO o TOTS signa
                        if (ModoVistiPlauConfig != null)//.ContainsKey("*"))
                            req.signLineList[linea].type = PF7.signLineType.VISTOBUENO;
                        else
                            req.signLineList[linea].type = PF7.signLineType.FIRMA;
                    }
                    else
                    {
                        if (ModoVistiPlauConfig != null)//.ContainsKey("*"))
                            req.signLineList[linea].type = PF7.signLineType.VISTOBUENO;
                        else
                            req.signLineList[linea].type = PF7.signLineType.FIRMA;
                    }
                }
                else
                    req.signLineList[linea].type = PF7.signLineType.FIRMA;

                req.signLineList[linea].typeSpecified = true;
                req.signLineList[linea].signerList = new PF7.signer[1];
                req.signLineList[linea].signerList[0] = new PF7.signer();
                req.signLineList[linea].signerList[0].userJob = signant;
                linea++;
            }

#if USERREMITENT
            PF7.user UsuariRemitent = new PF7.user();
            UsuariRemitent.identifier = "PFIRMA_PERSONAL";
            UsuariRemitent.name = "App de";
            UsuariRemitent.surname1 = "Gestió de Lots";
            UsuariRemitent.surname2 = "";

            req.remitterList = new PF7.user[1];
            req.remitterList[0] = UsuariRemitent;
#endif

#if LISTAAVISOS
            req.noticeList = new PF7.state[3];
            req.noticeList[0] = new PF7.state();
            req.noticeList[0].identifier = "FIRMADO";
            req.noticeList[1] = new PF7.state();
            req.noticeList[1].identifier = "DEVUELTO";
            req.noticeList[2] = new PF7.state();
            req.noticeList[2].identifier = "CADUCADO";
#endif
            if (true)
                req.signType = PF7.signType.CASCADA;
            else
                req.signType = PF7.signType.PARALELA;

            req.signTypeSpecified = true;
#if TIMESTAMP
            req.timestampInfo = new PF7.timestampInfo();
            req.timestampInfo.addTimestamp = true;
            req.timestampInfo.addTimestampSpecified = true;
#else
            req.timestampInfo = new PF7.timestampInfo();
            req.timestampInfo.addTimestamp = false;
            req.timestampInfo.addTimestampSpecified = false;
#endif
            req.requestStatus = new PF7.requestStatus();

            PF7.ModifyService ppp = new PF7.ModifyServiceClient();
            PF7.ModifyServiceClient CCC = new PF7.ModifyServiceClient();
            PF7.createRequest CR = new PF7.createRequest();
            PF7.authentication pfAuth = new PF7.authentication();

            pfAuth.userName = "PFIRMA_PERSONAL";
            pfAuth.password = "UDL41a8OiZDd1";
            req.application = "PFIRMA_PERSONAL";

            CR.request = req;
            CR.authentication = pfAuth;


            PF7.createRequest RequestHashNormal = new PF7.createRequest();
            RequestHashNormal.authentication = pfAuth;
            RequestHashNormal.request = req;


            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls | System.Net.SecurityProtocolType.Tls11 | System.Net.SecurityProtocolType.Tls12 | System.Net.SecurityProtocolType.Ssl3;
            PF7.createRequestResponse1 aa = null;
            string RequestID = "";
            PF7.createRequestResponse Resp = null;


            var binding = new CustomBinding();
            var mtomEncoder = new MtomMessageEncodingBindingElement(MessageVersion.Soap11, Encoding.UTF8);
            var transport = new HttpsTransportBindingElement(); // O HttpTransportBindingElement si no usas HTTPS
            binding.Elements.Add(mtomEncoder);
            binding.Elements.Add(transport);
            var endpoint = new System.ServiceModel.EndpointAddress("https://v5-ae-portasignatures7-preprod.udl.cat/pf/servicesv2/ModifyService?wsdl");
            try
            {
             
                    var client = new PF7.ModifyServiceClient(binding, endpoint);
                Resp = CCC.createRequest(RequestHashNormal);
                RequestID = Resp.requestId;
            }
            catch (Exception error)
            {
                ;
            }












        }

    }

}
