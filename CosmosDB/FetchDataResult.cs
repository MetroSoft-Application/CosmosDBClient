using System.Data;

namespace CosmosDBClient.CosmosDB
{
    /// <summary>
    /// CosmosDB����̃f�[�^�擾���ʂ�\���N���X
    /// </summary>
    public class FetchDataResult
    {
        /// <summary>
        /// �擾�����f�[�^
        /// </summary>
        public DataTable Data { get; }

        /// <summary>
        /// �����N�G�X�g�`���[�W (RU)
        /// </summary>
        public double TotalRequestCharge { get; }

        /// <summary>
        /// �h�L�������g��
        /// </summary>
        public int DocumentCount { get; }

        /// <summary>
        /// �y�[�W��
        /// </summary>
        public int PageCount { get; }

        /// <summary>
        /// �������� (�~���b)
        /// </summary>
        public long ElapsedMilliseconds { get; }

        /// <summary>
        /// �f�[�^�擾���ɔ��������G���[���
        /// </summary>
        public string ErrorMessage { get; }

        /// <summary>
        /// ���s�����N�G��������
        /// </summary>
        public string ExecutedQuery { get; }

        /// <summary>
        /// �擾�����f�[�^�̍s��
        /// </summary>
        public int RowCount => Data?.Rows.Count ?? 0;

        /// <summary>
        /// �擾�����f�[�^�̑��o�C�g��
        /// </summary>
        public long DataSizeInBytes { get; }

        /// <summary>
        /// �f�[�^�擾�̊J�n����
        /// </summary>
        public DateTime StartTime { get; }

        /// <summary>
        /// �f�[�^�擾�̏I������
        /// </summary>
        public DateTime EndTime { get; }

        /// <summary>
        /// ���ׂẴv���p�e�B���w�肷��R���X�g���N�^
        /// </summary>
        /// <param name="data">�擾�����f�[�^</param>
        /// <param name="totalRequestCharge">�����N�G�X�g�`���[�W (RU)</param>
        /// <param name="documentCount">�h�L�������g��</param>
        /// <param name="pageCount">�y�[�W��</param>
        /// <param name="elapsedMilliseconds">�������� (�~���b)</param>
        /// <param name="errorMessage">�f�[�^�擾���ɔ��������G���[���</param>
        /// <param name="executedQuery">���s�����N�G��������</param>
        /// <param name="dataSizeInBytes">�擾�����f�[�^�̑��o�C�g��</param>
        /// <param name="startTime">�f�[�^�擾�̊J�n����</param>
        /// <param name="endTime">�f�[�^�擾�̏I������</param>
        public FetchDataResult(
            DataTable data,
            double totalRequestCharge,
            int documentCount,
            int pageCount,
            long elapsedMilliseconds,
            string errorMessage,
            string executedQuery,
            long dataSizeInBytes,
            DateTime startTime,
            DateTime endTime)
        {
            Data = data ?? new DataTable();
            TotalRequestCharge = totalRequestCharge;
            DocumentCount = documentCount;
            PageCount = pageCount;
            ElapsedMilliseconds = elapsedMilliseconds;
            ErrorMessage = errorMessage;
            ExecutedQuery = executedQuery ?? string.Empty;
            DataSizeInBytes = dataSizeInBytes;
            StartTime = startTime;
            EndTime = endTime;
        }
    }
}
