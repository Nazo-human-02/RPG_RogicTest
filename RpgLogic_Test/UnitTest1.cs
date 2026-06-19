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
            var damageInfo = CreateDmgInfo();
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
            var damageInfo = CreateDmgInfo();
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
            var damageInfo = CreateDmgInfo();
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
            var damageInfo = CreateDmgInfo();
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
            var damageInfo = CreateDmgInfo();
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
            var damageInfo = CreateDmgInfo(fixedDamage: 100);
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
            var damageInfo = CreateDmgInfo();
            BattleCalculator battleCalculator = new BattleCalculator(randomProvider);
            //Act
            var (isCritical, damageValue) = battleCalculator.CalculateDamage(attacker, defender, damageInfo);
            //Assert
            Assert.True(isCritical);
            Assert.Equal(100, damageValue); //(Atk:100 - Def:50)*Cri:2 = 100
        }
        [Fact]
        public void CalculateDamage_クリティカル確定_レベル補正無し_乱数補正最大_クリティカル倍率2()
        {
            IRandomProvider randomProvider = new ReturnMaxProvider(floatLimit:0.9f);
            //Arrange
            var attacker = CreateStat(atk: 100, cri: 2, criPer: 100f);
            var defender = CreateStat(def: 50);
            var damageInfo = CreateDmgInfo();
            BattleCalculator battleCalculator = new BattleCalculator(randomProvider);
            //Act
            var (isCritical, damageValue) = battleCalculator.CalculateDamage(attacker, defender, damageInfo);
            //Assert
            Assert.True(isCritical);
            Assert.Equal(110, damageValue); //(Atk:100 - Def:50) = 50 + 10%乱数補正 = 55 * Cri:2 = 110
        }
        [Fact]
        public void CalculateDamage_クリティカル確定_レベル補正無し_乱数補正最小_クリティカル倍率2()
        {
            IRandomProvider randomProvider = new ReturnMinProvider(floatLowerLimit:0.02f);
            //Arrange
            var attacker = CreateStat(atk: 100, cri: 2, criPer: 100f);
            var defender = CreateStat(def: 50);
            var damageInfo = CreateDmgInfo();
            BattleCalculator battleCalculator = new BattleCalculator(randomProvider);
            //Act
            var (isCritical, damageValue) = battleCalculator.CalculateDamage(attacker, defender, damageInfo);
            //Assert
            Assert.True(isCritical);
            Assert.Equal(90, damageValue); //(Atk:100 - Def:50) = 50 - 10%乱数補正 = 45 * Cri:2 = 90
        }
        [Fact]
        public void CalculateDamage_クリティカル確定_レベル補正無し_乱数補正最小_クリティカル倍率2_ダメージ保証1()
        {
            IRandomProvider randomProvider = new ReturnMinProvider();
            //Arrange
            var attacker = CreateStat(atk: 5, cri: 2, criPer: 100f);
            var defender = CreateStat(def: 10);
            var damageInfo = CreateDmgInfo();
            BattleCalculator battleCalculator = new BattleCalculator(randomProvider);
            //Act
            var (isCritical, damageValue) = battleCalculator.CalculateDamage(attacker, defender, damageInfo);
            //Assert
            Assert.True(isCritical);
            Assert.Equal(2, damageValue); //(Atk:5 - Def:10) = -5 - 10%乱数補正 = 最低保証で1 * Cri:2 = 2
        }
        [Fact]
        public void CalculateDamage_クリティカル確定_レベル補正無し_乱数補正最大_クリティカル倍率2_ダメージ補正3倍_ダメージ保証有効()
        {
            IRandomProvider randomProvider = new ReturnMaxProvider(floatLimit:0.9f);
            //Arrange
            var attacker = CreateStat(atk: 5, cri: 2, criPer: 100f);
            var defender = CreateStat(def: 10);
            var damageInfo = CreateDmgInfo(damageMultiplier: 3f);
            BattleCalculator battleCalculator = new BattleCalculator(randomProvider);
            //Act
            var (isCritical, damageValue) = battleCalculator.CalculateDamage(attacker, defender, damageInfo);
            //Assert
            Assert.True(isCritical);
            Assert.Equal(6, damageValue); //Atk:5 - Def:10 = -5 + 乱数補正10% = -5.5 =>最低保証1 * ダメージ補正3倍 = 3 * Cri:2 = 6
        }

        [Fact]
        public void CalculateCriticalRate_レベル差無し_クリ率補正無し()
        {
            IRandomProvider randomProvider = new ReturnMaxProvider();
            //Arrange
            var attacker = CreateStat(criPer: 20f);
            var defender = CreateStat();
            BattleCalculator battleCalculator = new BattleCalculator(randomProvider);
            //Act
            float criticalRate = battleCalculator.CalculateCriticalRate(attacker, defender);
            //Assert
            Assert.Equal(20f, criticalRate); //クリ率は20%のまま
        }
        [Fact]
        public void CalculateCriticalRate_レベル差5_攻撃者優位_クリ率補正無し()
        {
            IRandomProvider randomProvider = new ReturnMaxProvider();
            //Arrange
            var attacker = CreateStat(criPer: 20f, level: 6);
            var defender = CreateStat(level:1);
            BattleCalculator battleCalculator = new BattleCalculator(randomProvider);
            //Act
            float criticalRate = battleCalculator.CalculateCriticalRate(attacker, defender);
            //Assert
            Assert.Equal(27.5f, criticalRate); //criPer:20 + レベル差5*1.5f = 27.5f
        }
        [Fact]
        public void CalculateCriticalRate_レベル差5_防御者優位_クリ率補正無し()
        {
            IRandomProvider randomProvider = new ReturnMaxProvider();
            //Arrange
            var attacker = CreateStat(criPer: 20f, level: 1);
            var defender = CreateStat(level:6);
            BattleCalculator battleCalculator = new BattleCalculator(randomProvider);
            //Act
            float criticalRate = battleCalculator.CalculateCriticalRate(attacker, defender);
            //Assert
            Assert.Equal(12.5f, criticalRate); //criPer:20 - レベル差5 * 1.5f = 12.5f
        }



        private BattleStat CreateStat(int atk = 0, int def = 0, int cri = 0, float criPer = 0f, int level = 1)
        {
            return new BattleStat
            {
                expSet = new ExpSet { CurrentLevel= level },
                baseStat = new BaseStat
                {
                    Atk = atk,
                    Def = def,
                    Cri = cri,
                    CriPer = criPer
                }
                
            };
        }

        private DamageInfo CreateDmgInfo(int fixedDamage = 0, float damageMultiplier = 1f)
        {
            return new DamageInfo() { FixedDamage = fixedDamage, DamageMultiplier = damageMultiplier };
        }
        
    }

    
}