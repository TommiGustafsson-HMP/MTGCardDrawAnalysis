using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization.Formatters;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace CardDrawAnalysis
{
    public enum ProbabilityType { Exact, AtLeast, AtMost }
    public enum MatchType { Bo1, Bo3 };
    public enum InverseType { Normal, Inverse };

    internal class Probabilities
    {
        private int _deckSize = 60;
        private int _minCardCount = 1;
        private int _maxCardCount = 32;
        private int _minCardsInHand = 1;
        private int _maxCardsInHand = 16;

        private double[][][] _drawChanceArray;
        private double[][][] _cardsInHandProbabilityArray;
        private double[][][][] _gameProbabilityArray;

        private double[][][] _cardsInHandProbabilityArrayAtLeast;
        private double[][][] _cardsInHandProbabilityArrayAtMost;

        public Probabilities(int? deckSize = null, int? minCardCount = null, int? maxCardCount = null, int? minCardsInHand = null, int? maxCardsInHand = null)
        {
            if(deckSize.HasValue)
                _deckSize = deckSize.Value;
            if(minCardCount.HasValue)
                _minCardCount = minCardCount.Value;
            if(maxCardCount.HasValue)
                _maxCardCount = maxCardCount.Value;
            if(minCardsInHand.HasValue)
                _minCardsInHand = minCardsInHand.Value;
            if(maxCardsInHand.HasValue)
                _maxCardsInHand = maxCardsInHand.Value;

            _drawChanceArray = new double[_maxCardCount + 1][][]; //0 to 28
            _cardsInHandProbabilityArray = new double[_maxCardCount + 1][][];
            _cardsInHandProbabilityArrayAtLeast = new double[_maxCardCount + 1][][];
            _cardsInHandProbabilityArrayAtMost = new double[_maxCardCount + 1][][];
            _gameProbabilityArray = new double[_maxCardCount + 1][][][];

            for (int i = 0; i <= _maxCardCount; i++)
            {
                _drawChanceArray[i] = new double[_maxCardCount + 1][]; //0 to 28
                _gameProbabilityArray[i] = new double[_maxCardsInHand + 1][][];
                for (int j = 0; j <= _maxCardCount; j++)
                {
                    _drawChanceArray[i][j] = new double[_maxCardCount + 1]; //0 to 28
                }
                for (int j = 0; j <= _maxCardsInHand; j++)
                {
                    _gameProbabilityArray[i][j] = new double[_maxCardsInHand + 1][];
                }
            }

            CalculateDrawChances();


        }



        private void CalculateDrawChances()
        {
            for (int specificCardsInDeck = 1; specificCardsInDeck <= _maxCardCount; specificCardsInDeck++)
            {
                for (int successes = 0; successes <= _maxCardCount; successes++)
                {
                    for (int failures = 0; failures <= _maxCardCount; failures++)
                    {
                        double drawChance = -1;
                        if (successes > specificCardsInDeck)
                        {
                            drawChance = -1; //Invalid
                        }
                        else if (failures > _deckSize - specificCardsInDeck)
                        {
                            drawChance = -1;
                        }
                        else
                        {
                            drawChance = (double)(specificCardsInDeck - successes) / (double)(_deckSize - successes - failures);
                        }
                        _drawChanceArray[specificCardsInDeck][successes][failures] = drawChance;
                    }
                }

                CalculateProbabilities(specificCardsInDeck);
                CalculateAtLeastAndAtMost(specificCardsInDeck);
                CheckProbabilitySums(specificCardsInDeck);
            }
        }

        public void CalculateProbabilities(int specificCardsInDeck)
        {
            for (int cardsInHand = _minCardsInHand; cardsInHand <= _maxCardsInHand; cardsInHand++)
            {
                for (int specificCardsInHand = 0; specificCardsInHand <= cardsInHand; specificCardsInHand++)
                {
                    int gameCount = (int)Math.Pow(2, cardsInHand);
                    _gameProbabilityArray[specificCardsInDeck][cardsInHand][specificCardsInHand] = new double[gameCount];
                    for (int j = 0; j < gameCount; j++)
                    {
                        _gameProbabilityArray[specificCardsInDeck][cardsInHand][specificCardsInHand][j] = 0d;
                    }

                }
            }



            for (int cardsInHand = _minCardsInHand; cardsInHand <= _maxCardsInHand; cardsInHand++)
            {
                for (int specificCardsInHand = 0; specificCardsInHand <= cardsInHand; specificCardsInHand++)
                {
                    if (specificCardsInHand <= cardsInHand)
                    {
                        int gameCount = (int)Math.Pow(2, cardsInHand);

                        for (int game = 0; game < gameCount; game++)
                        {
                            int successes = 0;
                            int failures = 0;
                            double gameChance = 1d;

                            //Previous Draw Chances
                            for (int bitPos = 1; bitPos <= cardsInHand; bitPos++)
                            {
                                int bitCompare = (int)Math.Pow(2, bitPos - 1);
                                bool bitValue = ((game & bitCompare) > 0);

                                double successChance = _drawChanceArray[specificCardsInDeck][successes][failures];
                                double chance = bitValue ? successChance : 1 - successChance;

                                gameChance *= chance;

                                if (bitValue)
                                {
                                    successes++;
                                }
                                else
                                {
                                    failures++;
                                }
                            }

                            _gameProbabilityArray[specificCardsInDeck][cardsInHand][specificCardsInHand][game] = gameChance;
                        }
                    }
                    else
                    {
                        //Invalid. Specific Cards In Hand cannot be > Cards In Hand
                        continue;
                    }

                }
            }

            _cardsInHandProbabilityArray[specificCardsInDeck] = new double[_maxCardsInHand + 1][];

            for (int cardsInHand = _minCardsInHand; cardsInHand <= _maxCardsInHand; cardsInHand++)
            {
                _cardsInHandProbabilityArray[specificCardsInDeck][cardsInHand] = new double[cardsInHand + 1];
                for (int specificCardsInHand = 0; specificCardsInHand <= cardsInHand; specificCardsInHand++)
                {
                    if (specificCardsInHand <= cardsInHand)
                    {
                        int gameCount = (int)Math.Pow(2, cardsInHand);

                        _cardsInHandProbabilityArray[specificCardsInDeck][cardsInHand][specificCardsInHand] = 0d;

                        for (int game = 0; game < gameCount; game++)
                        {
                            int successes = 0;
                            int failures = 0;

                            for (int bitPos = 1; bitPos <= cardsInHand; bitPos++)
                            {
                                int bitCompare = (int)Math.Pow(2, bitPos - 1);
                                bool bitValue = ((game & bitCompare) > 0);

                                if (bitValue)
                                {
                                    successes++;
                                }
                                else
                                {
                                    failures++;
                                }
                            }

                            if (successes == specificCardsInHand)
                            {
                                _cardsInHandProbabilityArray[specificCardsInDeck][cardsInHand][specificCardsInHand]
                                    += _gameProbabilityArray[specificCardsInDeck][cardsInHand][specificCardsInHand][game];
                            }
                        }
                    }
                    else
                    {
                        //Invalid
                        continue;
                    }
                }

            }


        }

        private void CheckProbabilitySums(int specificCardsInDeck)
        {
            for (int cardsInHand = _minCardsInHand; cardsInHand <= _maxCardsInHand; cardsInHand++)
            {
                for (int specificCardsInHand = 0; specificCardsInHand <= cardsInHand; specificCardsInHand++)
                {
                    double sum = 0d;
                    foreach (double chance in _gameProbabilityArray[specificCardsInDeck][cardsInHand][specificCardsInHand])
                    {
                        sum += chance;
                    }
                    sum = Math.Round(sum, 10);
                    if (sum != 1d)
                    {
                        throw new Exception("Probability sum is not 1. It is " + sum);
                    }
                }
            }
        }

        private void CalculateAtLeastAndAtMost(int specificCardsInDeck)
        {
            _cardsInHandProbabilityArrayAtLeast[specificCardsInDeck] = new double[_maxCardsInHand + 1][];
            _cardsInHandProbabilityArrayAtMost[specificCardsInDeck] = new double[_maxCardsInHand + 1][];

            for (int cardsInHand = _minCardsInHand; cardsInHand <= _maxCardsInHand; cardsInHand++)
            {
                _cardsInHandProbabilityArrayAtLeast[specificCardsInDeck][cardsInHand] = new double[cardsInHand + 1];
                _cardsInHandProbabilityArrayAtMost[specificCardsInDeck][cardsInHand] = new double[cardsInHand + 1];

                for (int specificCardsInHand = 0; specificCardsInHand <= cardsInHand; specificCardsInHand++)
                {
                    if (specificCardsInHand <= cardsInHand)
                    {
                        _cardsInHandProbabilityArrayAtMost[specificCardsInDeck][cardsInHand][specificCardsInHand] = 0d;
                        for (int specificCardInHandIndex = 0; specificCardInHandIndex <= specificCardsInHand; specificCardInHandIndex++)
                        {
                            _cardsInHandProbabilityArrayAtMost[specificCardsInDeck][cardsInHand][specificCardsInHand]
                                += _cardsInHandProbabilityArray[specificCardsInDeck][cardsInHand][specificCardInHandIndex];
                        }

                        _cardsInHandProbabilityArrayAtLeast[specificCardsInDeck][cardsInHand][specificCardsInHand] = 0d;
                        for (int specificCardInHandIndex = specificCardsInHand; specificCardInHandIndex <= cardsInHand; specificCardInHandIndex++)
                        {
                            _cardsInHandProbabilityArrayAtLeast[specificCardsInDeck][cardsInHand][specificCardsInHand]
                                += _cardsInHandProbabilityArray[specificCardsInDeck][cardsInHand][specificCardInHandIndex];
                        }
                    }
                }
            }
        }

        public double GetCardDrawProbability(int specificCardsInDeck, int cardsInHand, int specificCardsInHand, ProbabilityType probabilityType)
        {
            if(specificCardsInDeck > _maxCardCount || specificCardsInDeck < _minCardCount)
            {
                throw new ArgumentOutOfRangeException("specificCardsInDeck",
                    string.Format("specificCardsInDeck must be {0}-{1}.", _minCardCount, _maxCardCount));
            }

            if (cardsInHand > _maxCardsInHand || cardsInHand < _minCardsInHand)
            {
                throw new ArgumentOutOfRangeException("cardsInHand",
                    string.Format("cardsInHand must be {0}-{1}.", _minCardsInHand, _maxCardsInHand));
            }

            if (specificCardsInHand > cardsInHand)
            {
                throw new ArgumentOutOfRangeException("specificCardsInHand",
                    string.Format("specificCardsInHand must be equal to or less than cardsInHand"));
            }

            if (specificCardsInHand < 0)
            {
                throw new ArgumentOutOfRangeException("specificCardsInHand",
                    string.Format("specificCardsInHand equal to or greater than 0."));
            }

            switch(probabilityType)
            {
                case ProbabilityType.Exact:
                    return _cardsInHandProbabilityArray[specificCardsInDeck][cardsInHand][specificCardsInHand];
                case ProbabilityType.AtLeast:
                    return _cardsInHandProbabilityArrayAtLeast[specificCardsInDeck][cardsInHand][specificCardsInHand];
                case ProbabilityType.AtMost:
                    return _cardsInHandProbabilityArrayAtMost[specificCardsInDeck][cardsInHand][specificCardsInHand];
                default:
                    throw new NotImplementedException();
            }

        }

        public double[][][] GetDataArray(ProbabilityType probabilityType)
        {
            switch (probabilityType)
            {
                case ProbabilityType.Exact:
                    return _cardsInHandProbabilityArray;
                case ProbabilityType.AtLeast:
                    return _cardsInHandProbabilityArrayAtLeast;
                case ProbabilityType.AtMost:
                    return _cardsInHandProbabilityArrayAtMost;
                default:
                    throw new NotImplementedException();
            }
        }

        public void WriteData(string directory, string fileName, 
            ProbabilityType probabilityType, MatchType matchType = MatchType.Bo3, InverseType inverseType = InverseType.Normal)
        {
            if(string.IsNullOrWhiteSpace(directory))
            {
                throw new ArgumentNullException("directory");
            }

            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentNullException("fileName");
            }

            var dataArray = GetDataArray(probabilityType);
            CultureInfo ci = CultureInfo.InvariantCulture;

            DirectoryInfo dir = new DirectoryInfo(directory);
            if(!dir.Exists)
            {
                throw new ArgumentException("directory", string.Format("Directory {0} does not exist", directory));
            }

            for (int specificCardsInDeck = _minCardCount; specificCardsInDeck <= _maxCardCount; specificCardsInDeck++)
            {
                string fileName2 = fileName + "_ " + _deckSize + "_" + specificCardsInDeck + ".csv";
                FileInfo file = new FileInfo(Path.Combine(dir.FullName, fileName2));
                if (file.Exists)
                {
                    file.Delete();
                }

                using var writer = File.CreateText(file.FullName);

                StringBuilder[] sbArray = new StringBuilder[_maxCardsInHand + 1];
                StringBuilder sbHeader = new StringBuilder();
                sbHeader.Append(" ");

                int maxSpecificCardsInHand = _maxCardsInHand;
                if(probabilityType == ProbabilityType.AtMost && inverseType == InverseType.Inverse)
                {
                    maxSpecificCardsInHand--;
                }

                for (int cardsInHand = 0; cardsInHand <= maxSpecificCardsInHand; cardsInHand++)
                {
                    StringBuilder sb = new StringBuilder();
                    sbArray[cardsInHand] = sb;
                }

                for (int specificCardsInHand = 0; specificCardsInHand <= maxSpecificCardsInHand; specificCardsInHand++)
                {
                    var sb = sbArray[specificCardsInHand];
                    int specificCardsInHandNum = specificCardsInHand;

                    if (probabilityType == ProbabilityType.AtMost && inverseType == InverseType.Inverse)
                    {
                        specificCardsInHandNum++;
                    }

                    sb.Append(specificCardsInHandNum);
                }

                for (int cardsInHand = _minCardsInHand; cardsInHand <= _maxCardsInHand; cardsInHand++)
                {
                    sbHeader.Append(",").Append(cardsInHand);

                    for (int specificCardsInHand = 0; specificCardsInHand <= maxSpecificCardsInHand; specificCardsInHand++)
                    {
                        var sb = sbArray[specificCardsInHand];
                        sb.Append(",");
                        if (specificCardsInHand > cardsInHand)
                        {
                            sb.Append(" ");
                        }
                        else
                        {
                            var val = dataArray[specificCardsInDeck][cardsInHand][specificCardsInHand];
                            if(probabilityType == ProbabilityType.AtMost && matchType == MatchType.Bo1)
                            {
                                //Bo1 hand smoothing approximation
                                //Real formula is not known
                                val = Math.Pow(val, 2);
                            }
                            if(inverseType == InverseType.Inverse)
                            {
                                val = 1 - val;
                            }
                            
                            if(Math.Round(val, 10) == 0d)
                            {
                                sb.Append(" ");
                            }
                            else
                            {
                                sb.Append(val.ToString("F5", ci));
                            }
                            
                        }
                    }
                }

                writer.WriteLine(sbHeader.ToString());

                for (int specificCardsInHand = 0; specificCardsInHand <= maxSpecificCardsInHand; specificCardsInHand++)
                {
                    writer.WriteLine(sbArray[specificCardsInHand].ToString());
                }

                writer.Flush();
                writer.Close();
            }
        }
    }
}
