using NetSdrClientApp.Messages;

namespace NetSdrClientAppTests
{
    public class NetSdrMessageHelperTests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void GetControlItemMessageTest()
        {
            //Arrange
            var type = NetSdrMessageHelper.MsgTypes.Ack;
            var code = NetSdrMessageHelper.ControlItemCodes.ReceiverState;
            int parametersLength = 7500;

            //Act
            byte[] msg = NetSdrMessageHelper.GetControlItemMessage(type, code, new byte[parametersLength]);

            var headerBytes = msg.Take(2);
            var codeBytes = msg.Skip(2).Take(2);
            var parametersBytes = msg.Skip(4);

            var num = BitConverter.ToUInt16(headerBytes.ToArray());
            var actualType = (NetSdrMessageHelper.MsgTypes)(num >> 13);
            var actualLength = num - ((int)actualType << 13);
            var actualCode = BitConverter.ToInt16(codeBytes.ToArray());

            //Assert
            Assert.That(headerBytes.Count(), Is.EqualTo(2));
            Assert.That(msg.Length, Is.EqualTo(actualLength));
            Assert.That(type, Is.EqualTo(actualType));

            Assert.That(actualCode, Is.EqualTo((short)code));

            Assert.That(parametersBytes.Count(), Is.EqualTo(parametersLength));
        }

        [Test]
        public void GetDataItemMessageTest()
        {
            //Arrange
            var type = NetSdrMessageHelper.MsgTypes.DataItem2;
            int parametersLength = 7500;

            //Act
            byte[] msg = NetSdrMessageHelper.GetDataItemMessage(type, new byte[parametersLength]);

            var headerBytes = msg.Take(2);
            var parametersBytes = msg.Skip(2);

            var num = BitConverter.ToUInt16(headerBytes.ToArray());
            var actualType = (NetSdrMessageHelper.MsgTypes)(num >> 13);
            var actualLength = num - ((int)actualType << 13);

            //Assert
            Assert.That(headerBytes.Count(), Is.EqualTo(2));
            Assert.That(msg.Length, Is.EqualTo(actualLength));
            Assert.That(type, Is.EqualTo(actualType));

            Assert.That(parametersBytes.Count(), Is.EqualTo(parametersLength));
        }

        [Test]
        public void GetSamples8BitTest()
        {
            //Arrange
            ushort sampleSize = 8;
            byte[] body = new byte[] { 0x01, 0x02, 0x03, 0x04 }; // 4 samples

            //Act
            var samples = NetSdrMessageHelper.GetSamples(sampleSize, body).ToList();

            //Assert
            Assert.That(samples.Count, Is.EqualTo(4));
            Assert.That(samples[0], Is.EqualTo(1));
            Assert.That(samples[1], Is.EqualTo(2));
            Assert.That(samples[2], Is.EqualTo(3));
            Assert.That(samples[3], Is.EqualTo(4));
        }

        [Test]
        public void TranslateDataItemMessageTest()
        {
            //Arrange
            var type = NetSdrMessageHelper.MsgTypes.DataItem1;
            ushort expectedSequenceNum = 0x0201; // sequence number in little-endian
            byte[] bodyData = new byte[] { 0x03, 0x04, 0x05, 0x06 };
            // For DataItem messages: [header(2)][sequenceNumber(2)][body]
            var parameters = BitConverter.GetBytes(expectedSequenceNum).Concat(bodyData).ToArray();
            byte[] msg = NetSdrMessageHelper.GetDataItemMessage(type, parameters);

            //Act
            bool success = NetSdrMessageHelper.TranslateMessage(msg, out var actualType, out var actualCode, out var sequenceNum, out var body);

            //Assert
            Assert.That(success, Is.True);
            Assert.That(actualType, Is.EqualTo(type));
            Assert.That(actualCode, Is.EqualTo(NetSdrMessageHelper.ControlItemCodes.None));
            Assert.That(sequenceNum, Is.EqualTo(expectedSequenceNum));
            Assert.That(body, Is.EqualTo(bodyData));
        }

        [Test]
        public void GetSamples16BitTest()
        {
            //Arrange
            ushort sampleSize = 16;
            byte[] body = new byte[] { 0x01, 0x00, 0x02, 0x00, 0x03, 0x00 }; // 3 samples: 1, 2, 3

            //Act
            var samples = NetSdrMessageHelper.GetSamples(sampleSize, body).ToList();

            //Assert
            Assert.That(samples.Count, Is.EqualTo(3));
            Assert.That(samples[0], Is.EqualTo(1));
            Assert.That(samples[1], Is.EqualTo(2));
            Assert.That(samples[2], Is.EqualTo(3));
        }

        [Test]
        public void GetSamplesInvalidSizeTest()
        {
            //Arrange
            ushort sampleSize = 40; // Invalid size > 32 bits
            byte[] body = new byte[] { 0x01, 0x02 };

            //Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => NetSdrMessageHelper.GetSamples(sampleSize, body).ToList());
        }

        [Test]
        public void GetControlItemMessageWithLargeParametersTest()
        {
            //Arrange
            var type = NetSdrMessageHelper.MsgTypes.SetControlItem;
            var code = NetSdrMessageHelper.ControlItemCodes.IQOutputDataSampleRate;
            int parametersLength = 8189 - 4; // Max message length - header - code

            //Act
            byte[] msg = NetSdrMessageHelper.GetControlItemMessage(type, code, new byte[parametersLength]);

            //Assert
            Assert.That(msg.Length, Is.LessThanOrEqualTo(8191));
        }

        //TODO: add more NetSdrMessageHelper tests
    }
}