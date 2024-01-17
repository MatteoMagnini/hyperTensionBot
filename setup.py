from setuptools import setup, find_packages
import pathlib
import subprocess
import distutils.cmd

# current directory

here = pathlib.Path(__file__).parent.resolve()

version_file = here / 'VERSION'

# Get the long description from the README file
long_description = (here / 'README.md').read_text(encoding='utf-8')


def format_git_describe_version(version):
    if '-' in version:
        splitted = version.split('-')
        tag = splitted[0]
        index = f"dev{splitted[1]}"
        return f"{tag}.{index}"
    else:
        return version


def get_version_from_git():
    try:
        process = subprocess.run(["git", "describe"], cwd=str(here), check=True, capture_output=True)
        version = process.stdout.decode('utf-8').strip()
        version = format_git_describe_version(version)
        with version_file.open('w') as f:
            f.write(version)
        return version
    except subprocess.CalledProcessError:
        if version_file.exists():
            return version_file.read_text().strip()
        else:
            return '0.1.0'


version = get_version_from_git()


print(f"Detected version {version} from git describe")


class GetVersionCommand(distutils.cmd.Command):
    """A custom command to get the current project version inferred from git describe."""

    description = 'gets the project version from git describe'
    user_options = []

    def initialize_options(self):
        pass

    def finalize_options(self):
        pass

    def run(self):
        print(version)


setup(
    name='hyperTensionBot',  # Required
    version=version,
    description='Telegram bot for hypertension check',  # Optional
    license='Apache 2.0 License',
    long_description=long_description,
    long_description_content_type='text/markdown',
    url='https://github.com/MatteoMagnini/hyperTensionBot',
    author=[
         'Matteo Magnini',
         'Gianluca Aguzzi',
         'Sara Montagna',
    ],
    author_email=[
         'matteo.magnini@unibo.it',
         'gianluca.aguzzi@unibo.it',
         'sara.montagna@unibo.it'
    ],
    classifiers=[
        'Development Status :: 3 - Alpha',
        'Intended Audience :: Patients and Health Professionals',
        'Topic :: Biomedical Software :: Chatbot',
        'Topic :: Scientific/Engineering :: Artificial Intelligence',
        'License :: OSI Approved :: Apache Software License',
        'Programming Language :: C# :: 10.0',
        'Programming Language :: Python :: 3.10'
    ],
    keywords='hypertension, chatbot, deep learning, artificial intelligence, large language model', # Optional
    # package_dir={'': 'src'},  # Optional
    packages=find_packages(),  # Required
    include_package_data=True,
    python_requires='>=3.10.0',
    zip_safe = False,
    platforms = "Independant",
    project_urls={  # Optional
        'Bug Reports': 'https://github.com/MatteoMagnini/hyperTensionBot/issues',
        # 'Funding': 'https://donate.pypi.org',
        # 'Say Thanks!': 'http://saythanks.io/to/example',
        'Source': 'https://github.com/MatteoMagnini/hyperTensionBot',
    },
    cmdclass={
        'get_project_version': GetVersionCommand
    },
)