using Xunit;
namespace RpgLogic_Test
{
    public class BattleCalculatorTests
    {
        [Fact]
        public void CalculateDamage_クリティカル無し_乱数補正無し_TotalAtkとTotalDef参照のみ()
        {
            IRandomProvider randomProvider = new FixedRandomProvider(0, 1f);
            // Arrange
            var attacker = CreateStat(atk: 10);
            var defender = CreateStat(def: 5);

            DamageInfo damageInfo = new DamageInfo();
            BattleCalculator calculator = new BattleCalculator(randomProvider);
            // Act
            var (isCritical, damageValue) = calculator.CalculateDamage(attacker, defender, damageInfo);
            // Assert
            Assert.False(isCritical);
            Assert.Equal(5, damageValue); //Atk:10 - Def:5 = Damage:5
        }
        [Fact]
        public void CalculateDamage_クリティカル無し_乱数補正最大ならダメージに10パーセント加算()
        {
            IRandomProvider randomProvider = new ReturnMaxProvider();
            // Arrange
            var attacker = CreateStat(atk: 100);
            var defender = CreateStat(def: 50);

            DamageInfo damageInfo = new DamageInfo();
            BattleCalculator calculator = new BattleCalculator(randomProvider);
            // Act
            var (isCritical, damageValue) = calculator.CalculateDamage(attacker, defender, damageInfo);
            // Assert
            Assert.False(isCritical);
            Assert.Equal(55, damageValue); //Atk:100 - Def:50 = Damage:50 + 10% = 55
        }
        [Fact]
        public void CalculateDamage_クリティカル無し_乱数補正最小ならダメージに10パーセント減算()
        {
            IRandomProvider randomProvider = new ReturnMinProvider(floatLowerLimit:0.02f);
            // Arrange
            var attacker = CreateStat(atk: 100);
            var defender = CreateStat(def: 50);
            DamageInfo damageInfo = new DamageInfo();
            BattleCalculator calculator = new BattleCalculator(randomProvider);
            // Act
            var (isCritical, damageValue) = calculator.CalculateDamage(attacker, defender, damageInfo);
            // Assert
            Assert.False(isCritical);
            Assert.Equal(45, damageValue); //Atk:100 - Def:50 = Damage:50 - 10% = 45
        }
        [Fact]
        public void CalculateDamage_クリティカル無し_乱数補正無し_ダメージ保証1()
        {
            IRandomProvider randomProvider = new FixedRandomProvider(0, 1f);
            // Arrange
            var attacker = CreateStat(atk: 5);
            var defender = CreateStat(def: 10);
            DamageInfo damageInfo = new DamageInfo();
            BattleCalculator calculator = new BattleCalculator(randomProvider);
            // Act
            var (isCritical, damageValue) = calculator.CalculateDamage(attacker, defender, damageInfo);
            // Assert
            Assert.False(isCritical);
            Assert.Equal(1, damageValue); //Atk:5 - Def:10 = Damage:-5 => Damage保証で1
        }

        [Fact]
        public void CalculateDamage_クリティカル無し_乱数補正最小_ダメージ保証1()
        {
            IRandomProvider randomProvider = new ReturnMinProvider(floatLowerLimit:0.02f);
            // Arrange
            var attacker = CreateStat(atk: 5);
            var defender = CreateStat(def: 10);
            DamageInfo damageInfo = new DamageInfo();
            BattleCalculator calculator = new BattleCalculator(randomProvider);
            // Act
            var (isCritical, damageValue) = calculator.CalculateDamage(attacker, defender, damageInfo);
            // Assert
            Assert.False(isCritical);
            Assert.Equal(1, damageValue); //Atk:5 - Def:10 = Damage:-5 => Damage保証で1
        }

        [Fact]
        public void CalculateDamage_クリティカル無し_乱数補正あり_固定ダメージ指定()
        {
            IRandomProvider randomProvider = new ReturnMinProvider();
            // Arrange
            var attacker = CreateStat(atk: 5);
            var defender = CreateStat(def: 10);
            DamageInfo damageInfo = new DamageInfo() { FixedDamage = 100 };
            BattleCalculator calculator = new BattleCalculator(randomProvider);
            // Act
            var (isCritical, damageValue) = calculator.CalculateDamage(attacker, defender, damageInfo);
            // Assert
            Assert.False(isCritical);
            Assert.Equal(100, damageValue); //固定ダメージ指定が優先される
        }
        [Fact]
        public void CalculateDamage_クリティカル確定_レベル補正無し_乱数補正無し_クリティカル倍率2()
        {
            IRandomProvider randomProvider = new FixedRandomProvider(0, 0f);
            //Arrange
            var attacker = CreateStat(atk: 100, cri: 2, criPer: 100f);
            var defender = CreateStat(def: 50);
            DamageInfo damageInfo = new();
            BattleCalculator battleCalculator = new BattleCalculator(randomProvider);
            //Act
            var (isCritical, damageValue) = battleCalculator.CalculateDamage(attacker, defender, damageInfo);
            //Assert
            Assert.True(isCritical);
            Assert.Equal(100, damageValue); //(Atk:100 - Def:50)*Cri:2 = 100
        }











        private BattleStat CreateStat(int atk = 0, int def = 0, int cri = 0, float criPer = 0f)
        {
            return new BattleStat
            {
                baseStat = new BaseStat
                {
                    Atk = atk,
                    Def = def,
                    Cri = cri,
                    CriPer = criPer
                }
            };
        }

        
    }

    
}