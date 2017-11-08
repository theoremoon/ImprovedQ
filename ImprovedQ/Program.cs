using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImprovedQ
{
    class StateMachine
    {
        private int currentState;
        private int[,] nextStates;
        
        public StateMachine()
        {
            this.currentState = 0;
            this.nextStates = new int[,]
            {
                {1, 2, 0 },
                {3, 4, 1 },
                {4, 5, 2 },
                {6,7,3 },
                {7,8,4 },
                {8,9,5 },
                {10,11,6 },
                {11,12,7 },
                {12,13,8 },
                {13,14,9 },
                {15,16,10 },
                {16,17,11 },
                {17,18,12 },
                {18,19,13 },
                {19,20,14 },
                {0,0,15 },
                {-1,-1,16 },
                {-1,-1,17 },
                {-1,-1,18 },
                {-1,-1,19 },
                {-1,-1,20 },
            };
        }
        public bool IsGoal()
        {
            return this.currentState >= 15;
        }
        public double GetReward()
        {
            if (!this.IsGoal())
            {
                return 0;
            }
            return this.currentState * 10;
        }
        public void DoAction(int action)
        {
            this.currentState = this.nextStates[this.currentState,action];
        }
        public int GetCurrentState()
        {
            return this.currentState;
        }
    }

    class ImprovedQ
    {
        private List<List<double>> qValues; // Q値のリスト
        private readonly int actionNum; // 選びうる選択肢の数
        private readonly int stateNum;  // 状態の総数
        private readonly double discountRate; // 割引率
        private readonly double learningRate; // 学習率 

        // コンストラクタ。各種の値を初期化する
        public ImprovedQ(int actionNum, int stateNum, double learningRate, double discountRate)
        {
            this.actionNum = actionNum;
            this.stateNum = stateNum;
            this.learningRate = learningRate;
            this.discountRate = discountRate;

            this.InitQValues();
        }

        public void Show()
        {
            Console.Write(new string(' ', 4) + '|');
            for (int i = 0; i < this.stateNum; i++)
            {
                Console.Write("{0,4} ", "s" + i);
            }
            Console.WriteLine("\n" + new string('-', 5 * this.stateNum));
            for (int i = 0; i < actionNum; i++)
            {
                Console.Write("{0,4}|", "a" + i);
                for (int j = 0; j < stateNum; j++)
                {
                    Console.Write("{0,4} ", (int)this.qValues[j][i]);
                }
                Console.WriteLine();
            }
        }

        // qValuesを初期化する関数
        private void InitQValues(double initialValue = 0)
        {
            this.qValues = new List<List<double>>();
            for (int i = 0; i < stateNum; i++)
            {
                List<double> list = new List<double>();
                for (int j = 0; j < actionNum; j++)
                {
                    list.Add(initialValue);
                }
                qValues.Add(list);
            }
        }

        // 行動から学習する
        public void Update(List<KeyValuePair<int, int>> stateActionList, double reward)
        {
            if (stateActionList.Count <= 1) { return; }

            double rate = 1;
            for (int i = stateActionList.Count - 1; i >= 1; i--)
            {
                int beforeState = stateActionList[i - 1].Key;
                int action = stateActionList[i].Value;
                int afterState = stateActionList[i].Key;

                this.qValues[beforeState][action] = (1 - this.learningRate) * this.qValues[beforeState][action] + this.learningRate * reward * rate;
                rate *= this.discountRate;
            }
        }


        public void Update2(List<KeyValuePair<int, int>> stateActionList, double reward)
        {
            int i = stateActionList.Count - 1;
                int beforeState = stateActionList[i - 1].Key;
            int action = stateActionList[i].Value;
            int afterState = stateActionList[i].Key;

            this.qValues[beforeState][action] = (1 - this.learningRate) * this.qValues[beforeState][action] + this.learningRate * (reward + this.discountRate * this.qValues[afterState].Max());
        }

        // 現在の状態において、次に選択すべき行動を返す
        // e-グリーディ法
        public int GetNextAction(int state, Random random, double epsilon)
        {
            if (random is null)
            {
                random = new Random();
            }
            if (random.NextDouble() <= epsilon)
            {
                return random.Next(actionNum);
            }

            List<double> actionValues = qValues[state]; //行動の評価値
            List<KeyValuePair<double, int>> valuesWithIndex = new List<KeyValuePair<double, int>>();

            // actionの値をインデックスと紐付ける
            for (int i = 0; i < actionValues.Count; i++)
            {
                valuesWithIndex.Add(new KeyValuePair<double, int>(actionValues[i], i));
            }

            int action = valuesWithIndex
                .Where(x => x.Key == actionValues.Max()) // 評価が最大のアクションに絞る
                .OrderBy(x => random.Next()) // 残ったものをシャッフル
                .Take(1).ToArray()[0].Value; // 先頭の一個のもともとの添字を取得
            return action;
        }
    }
    class Program
    {
        static void Main(string[] args)
        {
            const int EpisodeNum = 100;
            const int N = 100;

            double[,] rates = new double[N, EpisodeNum];

            for (int n = 0; n < N; n++)
            {
                ImprovedQ improvedQ = new ImprovedQ(3, 21, 0.1, 0.1);
                Random random = new Random(1000+n);


                int actionCount = 0;
                double sumOfRewards = 0;
                // エピソードを回す
                for (int i = 0; i < EpisodeNum; i++)
                {
                    // 初期化
                    StateMachine stateMachine = new StateMachine();
                    double reward = 0;
                    List<KeyValuePair<int, int>> stateActionList = new List<KeyValuePair<int, int>>();

                    stateActionList.Add(new KeyValuePair<int, int>(stateMachine.GetCurrentState(), 0));
                    // 学習
                    while (!stateMachine.IsGoal())
                    {
                        // 現状から行動の選択
                        int currentState = stateMachine.GetCurrentState();
                        int act = improvedQ.GetNextAction(currentState, random, 0.2);

                        // 行動して報酬と結果を得る
                        stateMachine.DoAction(act);
                        reward = stateMachine.GetReward();
                        int nextState = stateMachine.GetCurrentState();

                        // 学習の状態を更新
                        stateActionList.Add(new KeyValuePair<int, int>(nextState, act));
                        //improvedQ.Update(stateActionList, reward);
                        improvedQ.Update2(stateActionList, reward);

                        actionCount++;
                        sumOfRewards += reward;
                    }
                    rates[n, i] = sumOfRewards / actionCount;
                }
            }

            for (int i = 0; i < EpisodeNum; i++)
            {
                for (int j = 0; j < N; j++)
                {
                    Console.Write("{0}, ", rates[j,i]);
                }
                Console.WriteLine();
            }
        }
    }
}
